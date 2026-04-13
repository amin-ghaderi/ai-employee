namespace AiEmployee.Application.Rag;

public sealed class VectorSearchResult
{
    public Guid MessageId { get; init; }
    public string ConversationId { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public double SimilarityScore { get; init; }
}
