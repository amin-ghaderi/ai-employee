using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Options;
using AiEmployee.Domain.Settings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiEmployee.Infrastructure.Options;

/// <summary>
/// Resolves <see cref="AppOptions.PublicBaseUrl"/> with precedence: database (<see cref="SystemSettingKeys.PublicBaseUrl"/>)
/// over configuration, cached for 60 seconds.
/// </summary>
public sealed class CachingPublicBaseUrlProvider : IPublicBaseUrlProvider
{
    /// <summary>Cache key for resolved public base URL (see Phase 2 spec).</summary>
    public const string CacheKey = "SystemSettings:PublicBaseUrl";

    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    private readonly ISystemSettingsRepository _systemSettings;
    private readonly IOptionsMonitor<AppOptions> _optionsMonitor;
    private readonly IMemoryCache _cache;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<CachingPublicBaseUrlProvider> _logger;

    public CachingPublicBaseUrlProvider(
        ISystemSettingsRepository systemSettings,
        IOptionsMonitor<AppOptions> optionsMonitor,
        IMemoryCache cache,
        IHostEnvironment environment,
        ILogger<CachingPublicBaseUrlProvider> logger)
    {
        _systemSettings = systemSettings;
        _optionsMonitor = optionsMonitor;
        _cache = cache;
        _environment = environment;
        _logger = logger;
    }

    /// <inheritdoc />
    public string? GetPublicBaseUrl()
    {
        if (_cache.TryGetValue(CacheKey, out PublicBaseUrlCacheEntry? cached) && cached is not null)
        {
            _logger.LogDebug("PublicBaseUrl cache hit (source={Source}).", cached.Source);
            return cached.Url;
        }

        var entry = ResolveUncached();
        _cache.Set(
            CacheKey,
            entry,
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration });

        LogResolution(entry);
        return entry.Url;
    }

    private void LogResolution(PublicBaseUrlCacheEntry entry)
    {
        if (entry.Url is null)
        {
            _logger.LogInformation("PublicBaseUrl: not configured (source={Source}).", entry.Source);
            return;
        }

        _logger.LogInformation(
            "PublicBaseUrl resolved from {Source} (urlLength={Length}).",
            entry.Source,
            entry.Url.Length);
    }

    private PublicBaseUrlCacheEntry ResolveUncached()
    {
        var dbRaw = _systemSettings
            .GetValueAsync(SystemSettingKeys.PublicBaseUrl, CancellationToken.None)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

        if (!string.IsNullOrWhiteSpace(dbRaw))
        {
            var normalized = dbRaw.Trim().TrimEnd('/');
            if (normalized.Length == 0)
                return new PublicBaseUrlCacheEntry(null, "Not configured");

            ValidateUri(normalized, sourceLabel: "Database");
            return new PublicBaseUrlCacheEntry(normalized, "Database");
        }

        var configRaw = _optionsMonitor.CurrentValue.PublicBaseUrl;
        if (string.IsNullOrWhiteSpace(configRaw))
            return new PublicBaseUrlCacheEntry(null, "Not configured");

        var normalizedConfig = configRaw.Trim().TrimEnd('/');
        if (normalizedConfig.Length == 0)
            return new PublicBaseUrlCacheEntry(null, "Not configured");

        ValidateUri(normalizedConfig, sourceLabel: "Configuration");
        return new PublicBaseUrlCacheEntry(normalizedConfig, "Configuration");
    }

    private void ValidateUri(string normalized, string sourceLabel)
    {
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
        {
            var origin = sourceLabel == "Database"
                ? "database system setting PublicBaseUrl"
                : "App:PublicBaseUrl";
            throw new InvalidOperationException(
                $"{origin} must be an absolute URI. Current value could not be parsed: '{TruncateForMessage(normalized)}'.");
        }

        if (_environment.IsProduction())
        {
            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"PublicBaseUrl from {sourceLabel} must use HTTPS in Production (Telegram and browser security requirements).");
            }
        }
    }

    private static string TruncateForMessage(string s, int max = 80) =>
        s.Length <= max ? s : s[..max] + "…";

    private sealed record PublicBaseUrlCacheEntry(string? Url, string Source);
}
