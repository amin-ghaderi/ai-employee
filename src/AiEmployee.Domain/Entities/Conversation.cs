namespace AiEmployee.Domain.Entities;

/// <summary>
/// An ordered collection of messages between participants (pure domain model).
/// </summary>
public sealed class Conversation
{
    public string Id { get; private set; } = string.Empty;
    public List<Message> Messages { get; private set; } = new();

    private Conversation()
    {
    }

    public Conversation(string id)
    {
        Id = id;
        Messages = new List<Message>();
    }

    public void AddMessage(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);
        Messages.Add(message);
    }
}
