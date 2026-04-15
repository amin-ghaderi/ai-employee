namespace AiEmployee.Domain.BotConfiguration;

public sealed class BotIntegration
{
    public Guid Id { get; private set; }
    public Guid BotId { get; private set; }
    public string Channel { get; private set; } = string.Empty;
    public string ExternalId { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; }

    public string? GatewayChannel { get; private set; }
    public string? GatewayExternalId { get; private set; }

    private BotIntegration()
    {
    }

    public BotIntegration(
        Guid id,
        Guid botId,
        string channel,
        string externalId,
        bool isEnabled,
        string? gatewayChannel = null,
        string? gatewayExternalId = null)
    {
        Id = id;
        BotId = botId;
        Channel = channel;
        ExternalId = externalId;
        IsEnabled = isEnabled;
        GatewayChannel = NormalizeOptional(gatewayChannel);
        GatewayExternalId = NormalizeOptional(gatewayExternalId);
    }

    public void Update(
        Guid botId,
        string channel,
        string externalId,
        bool isEnabled,
        string? gatewayChannel = null,
        string? gatewayExternalId = null)
    {
        BotId = botId;
        Channel = channel;
        ExternalId = externalId;
        IsEnabled = isEnabled;
        GatewayChannel = NormalizeOptional(gatewayChannel);
        GatewayExternalId = NormalizeOptional(gatewayExternalId);
    }

    public void SetEnabled(bool isEnabled)
    {
        IsEnabled = isEnabled;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
