namespace AiEmployee.Domain.Entities;

/// <summary>
/// An ordered collection of messages between participants (pure domain model).
/// </summary>
public sealed class Conversation
{
    public string Id { get; }
    public List<Message> Messages { get; }

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
