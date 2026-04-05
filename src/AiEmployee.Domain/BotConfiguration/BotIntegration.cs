namespace AiEmployee.Domain.BotConfiguration;

public sealed class BotIntegration
{
    public Guid Id { get; private set; }
    public Guid BotId { get; private set; }
    public string Channel { get; private set; } = string.Empty;
    public string ExternalId { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; }

    private BotIntegration()
    {
    }

    public BotIntegration(Guid id, Guid botId, string channel, string externalId, bool isEnabled)
    {
        Id = id;
        BotId = botId;
        Channel = channel;
        ExternalId = externalId;
        IsEnabled = isEnabled;
    }
}
