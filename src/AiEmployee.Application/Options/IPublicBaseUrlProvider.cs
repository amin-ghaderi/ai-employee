namespace AiEmployee.Application.Options;

/// <summary>Provides the configured public base URL for outbound-facing URLs (Telegram webhooks, etc.).</summary>
public interface IPublicBaseUrlProvider
{
    /// <summary>
    /// Returns the trimmed public base URL without a trailing slash, or <c>null</c> when <see cref="AppOptions.PublicBaseUrl"/> is unset or whitespace.
    /// In Production, throws if a non-empty URL is not absolute HTTPS.
    /// </summary>
    string? GetPublicBaseUrl();
}
