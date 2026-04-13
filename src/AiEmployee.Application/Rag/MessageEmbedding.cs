namespace AiEmployee.Application.Rag;

/// <summary>
/// Embedding payload for <see cref="Interfaces.IVectorStore"/> (not an EF entity).
/// </summary>
public sealed class MessageEmbedding
{
    public Guid Id { get; init; }
    public Guid MessageId { get; init; }
    public string ConversationId { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public float[] Embedding { get; init; } = Array.Empty<float>();
    public DateTime CreatedAt { get; init; }
}
