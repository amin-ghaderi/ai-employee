using AiEmployee.Application.Rag;

namespace AiEmployee.Application.Interfaces;

public interface IVectorStore
{
    Task StoreAsync(MessageEmbedding embedding, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] embedding,
        int topK,
        string? conversationId,
        CancellationToken cancellationToken = default);
}
