namespace AiEmployee.Domain.Entities;

/// <summary>
/// A single utterance in a conversation (pure domain model).
/// </summary>
public sealed class Message
{
    public Guid Id { get; private set; }
    public string ConversationId { get; private set; } = string.Empty;
    public string UserId { get; private set; } = string.Empty;
    public string? Username { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private Message()
    {
    }

    public Message(
        string conversationId,
        string userId,
        string text,
        string? username = null,
        string? firstName = null,
        string? lastName = null)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
            throw new ArgumentException("ConversationId is required.", nameof(conversationId));

        Id = Guid.NewGuid();
        ConversationId = conversationId;
        UserId = userId;
        Text = text;
        Username = username;
        FirstName = firstName;
        LastName = lastName;
        CreatedAt = DateTime.UtcNow;
    }
}
