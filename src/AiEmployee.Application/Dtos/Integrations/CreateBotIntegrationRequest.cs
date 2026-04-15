namespace AiEmployee.Application.Dtos.Integrations;

public sealed class CreateBotIntegrationRequest
{
    public Guid BotId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;

    public string? GatewayChannel { get; set; }
    public string? GatewayExternalId { get; set; }
}
