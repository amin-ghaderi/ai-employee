namespace AiEmployee.Domain.Entities;

/// <summary>
/// Identifies who produced a <see cref="Message"/> in a conversation.
/// </summary>
public enum MessageSpeaker
{
    User = 0,
    Assistant = 1,
}
