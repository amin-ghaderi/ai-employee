namespace AiEmployee.Infrastructure.Integrations.WhatsApp;

/// <summary>Configuration for Meta WhatsApp Cloud API (Graph) used by <see cref="WhatsAppIntegrationProvider"/>.</summary>
public sealed class WhatsAppSettings
{
    public const string SectionName = "WhatsApp";

    /// <summary>System user or permanent access token for Graph API calls.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Phone number ID from the Meta developer app (used in subscribed_apps path).</summary>
    public string PhoneNumberId { get; set; } = string.Empty;

    /// <summary>Token Meta sends during webhook GET verification; must match query <c>hub.verify_token</c>.</summary>
    public string VerifyToken { get; set; } = string.Empty;

    public string GraphApiVersion { get; set; } = "v19.0";
}
