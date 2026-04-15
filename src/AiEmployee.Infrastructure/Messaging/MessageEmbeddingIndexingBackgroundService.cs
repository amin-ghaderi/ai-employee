using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Options;
using AiEmployee.Application.Rag;
using AiEmployee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiEmployee.Infrastructure.Messaging;

/// <summary>
/// Dequeues persisted message ids, generates embeddings when RAG is enabled, and stores them in PostgreSQL.
/// </summary>
public sealed class MessageEmbeddingIndexingBackgroundService : BackgroundService
{
    private readonly MessageEmbeddingWorkQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MessageEmbeddingIndexingBackgroundService> _logger;

    public MessageEmbeddingIndexingBackgroundService(
        MessageEmbeddingWorkQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<MessageEmbeddingIndexingBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reader = _queue.Reader;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            while (reader.TryRead(out var messageId))
            {
                try
                {
                    await ProcessOneAsync(messageId, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Message embedding indexing failed | messageId={MessageId}", messageId);
                }
            }
        }
    }

    private async Task ProcessOneAsync(Guid messageId, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var rag = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<RagOptions>>().CurrentValue;
        if (!rag.Enabled)
            return;

        var embeddingOpts = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<EmbeddingOptions>>().CurrentValue;
        if (string.Equals(embeddingOpts.Provider, "Placeholder", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(embeddingOpts.Endpoint))
        {
            return;
        }

        var conversationRepository = scope.ServiceProvider.GetRequiredService<IConversationRepository>();
        var message = await conversationRepository.GetMessageByIdAsync(messageId, cancellationToken).ConfigureAwait(false);
        if (message is null)
        {
            _logger.LogDebug("Embedding job skipped (message not found) | messageId={MessageId}", messageId);
            return;
        }

        if (string.IsNullOrWhiteSpace(message.Text))
            return;

        var db = scope.ServiceProvider.GetRequiredService<AiEmployeeDbContext>();
        if (await db.MessageEmbeddings.AnyAsync(e => e.MessageId == messageId, cancellationToken)
                .ConfigureAwait(false))
        {
            return;
        }

        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
        var vector = await embeddingService.GenerateEmbeddingAsync(message.Text, cancellationToken).ConfigureAwait(false);

        if (vector.Length != 1536 || IsAllZeros(vector))
        {
            _logger.LogDebug(
                "Embedding job skipped (invalid dimensions or zero vector) | messageId={MessageId} length={Length}",
                messageId,
                vector.Length);
            return;
        }

        var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();
        var payload = new MessageEmbedding
        {
            Id = Guid.NewGuid(),
            MessageId = message.Id,
            ConversationId = message.ConversationId,
            Content = message.Text,
            Embedding = vector,
            CreatedAt = DateTime.UtcNow,
        };

        await vectorStore.StoreAsync(payload, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsAllZeros(float[] vector)
    {
        foreach (var x in vector)
        {
            if (x != 0f)
                return false;
        }

        return true;
    }
}
