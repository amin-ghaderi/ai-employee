namespace AiEmployee.Application.Dtos.Integrations;

public sealed class BotIntegrationDto
{
    public Guid Id { get; set; }
    public Guid BotId { get; set; }
    public string Channel { get; set; } = string.Empty;

    /// <summary>Canonical provider key when <see cref="Channel"/> maps to a known provider (e.g. telegram); null when unknown.</summary>
    public string? Provider { get; set; }

    public string ExternalId { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }

    /// <summary>
    /// True when this integration can participate in webhook admin flows (has a non-empty external id).
    /// </summary>
    public bool SupportsWebhook { get; set; }
}
