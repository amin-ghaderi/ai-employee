using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Rag;

namespace AiEmployee.Infrastructure.AI;

/// <summary>
/// No-op vector store when PostgreSQL / pgvector is not the active provider.
/// </summary>
public sealed class NullVectorStore : IVectorStore
{
    public Task StoreAsync(MessageEmbedding embedding, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] embedding,
        int topK,
        string? conversationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<VectorSearchResult>>(Array.Empty<VectorSearchResult>());
}
