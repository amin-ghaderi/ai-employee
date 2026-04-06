namespace AiEmployee.Application.Dtos.Integrations;

public sealed class UpdateBotIntegrationRequest
{
    public Guid? BotId { get; set; }
    public string? Channel { get; set; }
    public string? ExternalId { get; set; }
    public bool? IsEnabled { get; set; }
}
