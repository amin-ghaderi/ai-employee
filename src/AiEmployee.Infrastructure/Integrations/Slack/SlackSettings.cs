namespace AiEmployee.Infrastructure.Integrations.Slack;

/// <summary>Slack app credentials for Events API signature verification and future Web API calls.</summary>
public sealed class SlackSettings
{
    public const string SectionName = "Slack";

    public string BotToken { get; set; } = string.Empty;

    /// <summary>Signing secret from Slack app &quot;Basic Information&quot; (verifies <c>X-Slack-Signature</c>).</summary>
    public string SigningSecret { get; set; } = string.Empty;

    public string AppId { get; set; } = string.Empty;
}
