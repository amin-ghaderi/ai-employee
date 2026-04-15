using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Options;
using AiEmployee.Application.Rag;
using AiEmployee.Infrastructure.Persistence;
using AiEmployee.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace AiEmployee.Infrastructure.AI;

public sealed class PgVectorStore : IVectorStore
{
    private readonly AiEmployeeDbContext _db;
    private readonly IOptions<RagOptions> _ragOptions;
    private readonly ILogger<PgVectorStore> _logger;

    public PgVectorStore(
        AiEmployeeDbContext db,
        IOptions<RagOptions> ragOptions,
        ILogger<PgVectorStore> logger)
    {
        _db = db;
        _ragOptions = ragOptions;
        _logger = logger;
    }

    public async Task StoreAsync(MessageEmbedding embedding, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(embedding);

        if (!_ragOptions.Value.Enabled)
        {
            _logger.LogDebug("PgVectorStore.StoreAsync skipped (RAG disabled).");
            return;
        }

        if (embedding.Embedding.Length != 1536)
        {
            _logger.LogWarning(
                "PgVectorStore.StoreAsync: expected 1536 dimensions, got {Count}; skipping store.",
                embedding.Embedding.Length);
            return;
        }

        if (await _db.MessageEmbeddings.AnyAsync(e => e.MessageId == embedding.MessageId, cancellationToken)
                .ConfigureAwait(false))
        {
            _logger.LogDebug(
                "PgVectorStore.StoreAsync skipped (embedding already exists for message {MessageId}).",
                embedding.MessageId);
            return;
        }

        var row = new MessageEmbeddingEntity
        {
            Id = embedding.Id == Guid.Empty ? Guid.NewGuid() : embedding.Id,
            MessageId = embedding.MessageId,
            ConversationId = embedding.ConversationId,
            Content = embedding.Content,
            Embedding = new Vector(embedding.Embedding),
            CreatedAt = embedding.CreatedAt == default ? DateTime.UtcNow : embedding.CreatedAt,
        };

        _db.MessageEmbeddings.Add(row);
        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _logger.LogDebug(
                ex,
                "PgVectorStore.StoreAsync skipped (concurrent insert for message {MessageId}).",
                embedding.MessageId);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation;

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] embedding,
        int topK,
        string? conversationId,
        CancellationToken cancellationToken = default)
    {
        if (!_ragOptions.Value.Enabled)
            return Array.Empty<VectorSearchResult>();

        if (embedding.Length != 1536)
        {
            _logger.LogWarning(
                "PgVectorStore.SearchAsync: expected 1536 dimensions, got {Count}.",
                embedding.Length);
            return Array.Empty<VectorSearchResult>();
        }

        var queryVector = new Vector(embedding);
        var take = Math.Clamp(topK, 1, _ragOptions.Value.MaxVectorResults);
        var minSim = _ragOptions.Value.MinSimilarity;

        var q = _db.MessageEmbeddings.AsNoTracking().Where(e => e.Embedding != null);

        if (!string.IsNullOrWhiteSpace(conversationId))
            q = q.Where(e => e.ConversationId == conversationId);

        var ranked = await q
            .OrderBy(e => e.Embedding!.CosineDistance(queryVector))
            .Take(take)
            .Select(e => new
            {
                e.MessageId,
                e.ConversationId,
                e.Content,
                Distance = e.Embedding!.CosineDistance(queryVector),
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var results = new List<VectorSearchResult>(ranked.Count);
        foreach (var row in ranked)
        {
            var similarity = 1.0 - row.Distance / 2.0;
            if (similarity < minSim)
                continue;

            results.Add(new VectorSearchResult
            {
                MessageId = row.MessageId,
                ConversationId = row.ConversationId,
                Content = row.Content,
                SimilarityScore = similarity,
            });
        }

        return results;
    }
}
