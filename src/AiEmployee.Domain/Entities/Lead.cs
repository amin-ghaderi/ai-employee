namespace AiEmployee.Domain.Entities;

/// <summary>
/// Captured lead / qualification data for a user (pure domain model).
/// </summary>
public sealed class Lead
{
    public string Id { get; private set; } = string.Empty;
    public string UserId { get; private set; } = string.Empty;
    public Dictionary<string, string> Answers { get; private set; } = new();
    public string UserType { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty;
    public string Potential { get; set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private Lead()
    {
    }

    public Lead(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required.", nameof(userId));

        Id = Guid.NewGuid().ToString();
        UserId = userId;
        CreatedAt = DateTime.UtcNow;
        Answers = new Dictionary<string, string>();
    }
}
