namespace AiEmployee.Domain.BotConfiguration;

public sealed class Bot
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public BotChannel Channel { get; private set; }
    public string ExternalIntegrationId { get; private set; } = string.Empty;
    public Guid PersonaId { get; private set; }
    public Guid BehaviorId { get; private set; }
    public Guid LanguageProfileId { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private Bot()
    {
    }

    public Bot(
        Guid id,
        string name,
        BotChannel channel,
        string externalIntegrationId,
        Guid personaId,
        Guid behaviorId,
        Guid languageProfileId,
        bool isEnabled,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt = null)
    {
        Id = id;
        Name = name;
        Channel = channel;
        ExternalIntegrationId = externalIntegrationId;
        PersonaId = personaId;
        BehaviorId = behaviorId;
        LanguageProfileId = languageProfileId;
        IsEnabled = isEnabled;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public void Update(string name, bool isEnabled, DateTimeOffset updatedAt)
    {
        Name = name;
        IsEnabled = isEnabled;
        UpdatedAt = updatedAt;
    }

    public void Assign(
        Guid personaId,
        Guid behaviorId,
        Guid languageProfileId,
        DateTimeOffset updatedAt)
    {
        PersonaId = personaId;
        BehaviorId = behaviorId;
        LanguageProfileId = languageProfileId;
        UpdatedAt = updatedAt;
    }

    public void SetEnabled(bool isEnabled, DateTimeOffset updatedAt)
    {
        IsEnabled = isEnabled;
        UpdatedAt = updatedAt;
    }
}
