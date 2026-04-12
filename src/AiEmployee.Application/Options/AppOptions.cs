namespace AiEmployee.Application.Options;

/// <summary>Application-level settings (public URL for webhooks, callbacks, etc.).</summary>
public sealed class AppOptions
{
    public const string SectionName = "App";

    /// <summary>
    /// Public origin of this API as seen by external systems (e.g. <c>https://your-ngrok-subdomain.ngrok-free.app</c> or production domain).
    /// Used to build Telegram webhook URLs. No trailing slash required.
    /// </summary>
    public string? PublicBaseUrl { get; set; }
}
