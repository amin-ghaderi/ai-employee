namespace AiEmployee.Domain.Entities;

/// <summary>
/// A single utterance in a conversation (pure domain model).
/// </summary>
public sealed class Message
{
    public string UserId { get; }
    public string? Username { get; }
    public string? FirstName { get; }
    public string? LastName { get; }
    public string Text { get; }
    public DateTime CreatedAt { get; }

    public Message(
        string userId,
        string text,
        string? username = null,
        string? firstName = null,
        string? lastName = null)
    {
        UserId = userId;
        Text = text;
        Username = username;
        FirstName = firstName;
        LastName = lastName;
        CreatedAt = DateTime.UtcNow;
    }
}
