namespace AiEmployee.Domain.Entities;

public class Conversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = string.Empty;
    public List<Message> Messages { get; set; } = new();
}
