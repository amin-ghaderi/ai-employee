namespace AiEmployee.Domain.Entities;

public class Message
{
    public string UserId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
