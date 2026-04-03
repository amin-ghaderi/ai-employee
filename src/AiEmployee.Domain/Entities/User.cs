namespace AiEmployee.Domain.Entities;

/// <summary>
/// Telegram user profile (pure domain model).
/// </summary>
public sealed class User
{
    public string Id { get; }
    public string? Username { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public DateTime JoinedAt { get; }
    public DateTime LastActiveAt { get; private set; }
    public int MessagesCount { get; private set; }
    public double EngagementScore { get; private set; }
    public List<string> Tags { get; }

    public User(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id is required.", nameof(id));

        Id = id;
        JoinedAt = DateTime.UtcNow;
        LastActiveAt = DateTime.UtcNow;
        Tags = new List<string>();
        MessagesCount = 0;
        EngagementScore = 0;
    }

    public void RegisterMessage()
    {
        MessagesCount++;
        LastActiveAt = DateTime.UtcNow;
        RecalculateEngagement();
        UpdateTags();
    }

    public void RecalculateEngagement()
    {
        var now = DateTime.UtcNow;
        var days = (now - JoinedAt).TotalDays;

        if (days <= 0)
        {
            EngagementScore = MessagesCount > 0 ? 1.0 : 0.0;
            return;
        }

        var messagesPerDay = MessagesCount / days;

        // simple normalization (tweak later)
        EngagementScore = Math.Min(1.0, messagesPerDay / 20.0);
    }

    public void UpdateTags()
    {
        Tags.Clear();

        var now = DateTime.UtcNow;

        if ((now - JoinedAt).TotalHours < 48)
            Tags.Add("new");

        if (MessagesCount >= 10)
            Tags.Add("active");

        if ((now - LastActiveAt).TotalHours > 72)
            Tags.Add("inactive");

        if (EngagementScore > 0.7)
            Tags.Add("high_engagement");
    }

    public void UpdateProfile(string? username, string? firstName, string? lastName)
    {
        if (username is not null)
            Username = username;
        if (firstName is not null)
            FirstName = firstName;
        if (lastName is not null)
            LastName = lastName;
    }
}
