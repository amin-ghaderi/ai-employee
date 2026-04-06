namespace AiEmployee.Application.Dtos.Integrations;

public sealed class BotIntegrationDto
{
    public Guid Id { get; set; }
    public Guid BotId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}
