namespace AiEmployee.Domain.Entities;

/// <summary>
/// Persisted AI judge outcome for a single request (pure domain model).
/// </summary>
public sealed class Judgment
{
    public Guid Id { get; private set; }
    public string ConversationId { get; private set; } = string.Empty;
    public string UserId { get; private set; } = string.Empty;
    public string InputText { get; private set; } = string.Empty;
    public string Winner { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private Judgment()
    {
    }

    public Judgment(string conversationId, string userId, string inputText, string winner, string reason)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
            throw new ArgumentException("ConversationId is required.", nameof(conversationId));
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required.", nameof(userId));

        Id = Guid.NewGuid();
        ConversationId = conversationId;
        UserId = userId;
        InputText = inputText ?? string.Empty;
        Winner = winner ?? string.Empty;
        Reason = reason ?? string.Empty;
        CreatedAt = DateTime.UtcNow;
    }
}
