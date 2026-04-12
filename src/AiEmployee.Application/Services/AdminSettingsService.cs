using AiEmployee.Application.Dtos.Settings;
using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Options;
using AiEmployee.Domain.Settings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AiEmployee.Application.Services;

public sealed class AdminSettingsService : IAdminSettingsService
{
    /// <summary>Must match the cache key used by <c>CachingPublicBaseUrlProvider</c> (Phase 2).</summary>
    private const string PublicBaseUrlCacheKey = "SystemSettings:PublicBaseUrl";

    private readonly ISystemSettingsRepository _systemSettings;
    private readonly IPublicBaseUrlProvider _publicBaseUrl;
    private readonly IMemoryCache _cache;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<AdminSettingsService> _logger;

    public AdminSettingsService(
        ISystemSettingsRepository systemSettings,
        IPublicBaseUrlProvider publicBaseUrl,
        IMemoryCache cache,
        IHostEnvironment environment,
        ILogger<AdminSettingsService> logger)
    {
        _systemSettings = systemSettings;
        _publicBaseUrl = publicBaseUrl;
        _cache = cache;
        _environment = environment;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<PublicBaseUrlDto> GetPublicBaseUrlAsync(CancellationToken cancellationToken = default)
    {
        var effective = _publicBaseUrl.GetPublicBaseUrl();
        return Task.FromResult(new PublicBaseUrlDto(effective));
    }

    /// <inheritdoc />
    public async Task<PublicBaseUrlDto> SetPublicBaseUrlAsync(string? url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            await ClearPublicBaseUrlAsync(cancellationToken).ConfigureAwait(false);
            return new PublicBaseUrlDto(_publicBaseUrl.GetPublicBaseUrl());
        }

        var normalized = url.Trim().TrimEnd('/');
        if (normalized.Length == 0)
        {
            await ClearPublicBaseUrlAsync(cancellationToken).ConfigureAwait(false);
            return new PublicBaseUrlDto(_publicBaseUrl.GetPublicBaseUrl());
        }

        ValidateNormalizedUrl(normalized);

        await _systemSettings
            .SetValueAsync(SystemSettingKeys.PublicBaseUrl, normalized, cancellationToken)
            .ConfigureAwait(false);

        InvalidatePublicBaseUrlCache();

        _logger.LogInformation(
            "PublicBaseUrl database override updated | masked={Masked}",
            MaskForLog(normalized));

        return new PublicBaseUrlDto(_publicBaseUrl.GetPublicBaseUrl());
    }

    /// <inheritdoc />
    public async Task ClearPublicBaseUrlAsync(CancellationToken cancellationToken = default)
    {
        await _systemSettings
            .SetValueAsync(SystemSettingKeys.PublicBaseUrl, null, cancellationToken)
            .ConfigureAwait(false);

        InvalidatePublicBaseUrlCache();

        _logger.LogInformation(
            "PublicBaseUrl database override cleared | effectiveMasked={Masked}",
            MaskForLog(_publicBaseUrl.GetPublicBaseUrl()));
    }

    private void InvalidatePublicBaseUrlCache() =>
        _cache.Remove(PublicBaseUrlCacheKey);

    private void ValidateNormalizedUrl(string normalized)
    {
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException(
                $"PublicBaseUrl must be an absolute URI. The value could not be parsed: '{TruncateForMessage(normalized)}'.",
                nameof(normalized));
        }

        if (_environment.IsProduction())
        {
            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "PublicBaseUrl must use HTTPS in Production (Telegram and browser security requirements).",
                    nameof(normalized));
            }
        }
    }

    private static string MaskForLog(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "(not configured)";

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return "(invalid)";

        return $"{uri.Scheme}://{uri.Host}/***";
    }

    private static string TruncateForMessage(string s, int max = 80) =>
        s.Length <= max ? s : s[..max] + "…";
}
