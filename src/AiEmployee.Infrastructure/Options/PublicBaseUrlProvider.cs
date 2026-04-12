using AiEmployee.Application.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AiEmployee.Infrastructure.Options;

public sealed class PublicBaseUrlProvider : IPublicBaseUrlProvider
{
    private readonly IOptions<AppOptions> _options;
    private readonly IHostEnvironment _environment;

    public PublicBaseUrlProvider(IOptions<AppOptions> options, IHostEnvironment environment)
    {
        _options = options;
        _environment = environment;
    }

    public string? GetPublicBaseUrl()
    {
        var raw = _options.Value.PublicBaseUrl;
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var normalized = raw.Trim().TrimEnd('/');
        if (normalized.Length == 0)
            return null;

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException(
                $"App:PublicBaseUrl must be an absolute URI. Current value could not be parsed: '{TruncateForMessage(raw)}'.");
        }

        if (_environment.IsProduction())
        {
            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "App:PublicBaseUrl must use HTTPS in Production (Telegram and browser security requirements).");
            }
        }

        return normalized;
    }

    private static string TruncateForMessage(string s, int max = 80) =>
        s.Length <= max ? s : s[..max] + "…";
}
