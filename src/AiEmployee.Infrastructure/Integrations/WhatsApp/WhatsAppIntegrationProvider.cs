using System.Net.Http.Headers;
using System.Net.Http.Json;
using AiEmployee.Application.Integrations;
using AiEmployee.Application.Integrations.Providers;
using AiEmployee.Domain.BotConfiguration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiEmployee.Infrastructure.Integrations.WhatsApp;

/// <summary>
/// Meta WhatsApp Cloud API: admin &quot;sync&quot; calls Graph <c>POST /{phone-number-id}/subscribed_apps</c>.
/// <see cref="BotIntegration.ExternalId"/> stores the public HTTPS callback URL you configure in Meta (used for status display and validation).
/// </summary>
public sealed class WhatsAppIntegrationProvider : IIntegrationProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<WhatsAppIntegrationProvider> _logger;

    public WhatsAppIntegrationProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<WhatsAppSettings> options,
        ILogger<WhatsAppIntegrationProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public string ProviderId => IntegrationProviders.WhatsApp;

    /// <inheritdoc />
    public bool SupportsWebhookLifecycle => true;

    private string GraphApiBaseUrl
    {
        get
        {
            var v = _settings.GraphApiVersion.Trim().TrimStart('/');
            return $"https://graph.facebook.com/{v}";
        }
    }

    /// <inheritdoc />
    public async Task<IntegrationWebhookSyncResult> SyncWebhookAsync(
        BotIntegration integration,
        CancellationToken cancellationToken)
    {
        var url = integration.ExternalId?.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            return IntegrationWebhookSyncResult.Failed(
                IntegrationWebhookFailureCategory.BadRequestGuard,
                "Webhook callback URL is missing.");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var callbackUri) || callbackUri.Scheme != Uri.UriSchemeHttps)
        {
            return IntegrationWebhookSyncResult.Failed(
                IntegrationWebhookFailureCategory.BadRequestGuard,
                "Webhook callback URL must be an absolute HTTPS URI.");
        }

        if (string.IsNullOrWhiteSpace(_settings.AccessToken) || string.IsNullOrWhiteSpace(_settings.PhoneNumberId))
        {
            return IntegrationWebhookSyncResult.Failed(
                IntegrationWebhookFailureCategory.BadRequestGuard,
                "WhatsApp configuration is incomplete. Set WhatsApp:AccessToken and WhatsApp:PhoneNumberId.");
        }

        try
        {
            var requestUrl = $"{GraphApiBaseUrl}/{_settings.PhoneNumberId.Trim()}/subscribed_apps";
            var client = _httpClientFactory.CreateClient("WhatsApp");
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.AccessToken.Trim());
            request.Content = JsonContent.Create(new { });

            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "whatsapp_subscribed_apps_failed | status={Status} body={Body}",
                    (int)response.StatusCode,
                    body);

                return IntegrationWebhookSyncResult.Failed(
                    IntegrationWebhookFailureCategory.UpstreamProviderError,
                    "Failed to subscribe WhatsApp app for webhook delivery.",
                    (int)response.StatusCode,
                    body,
                    url);
            }

            return IntegrationWebhookSyncResult.Ok(url, "WhatsApp subscribed_apps succeeded.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "whatsapp_webhook_sync_error | integrationId={IntegrationId}", integration.Id);
            return IntegrationWebhookSyncResult.Failed(
                IntegrationWebhookFailureCategory.InternalError,
                "Unexpected error while syncing WhatsApp webhook.");
        }
    }

    /// <inheritdoc />
    public Task<IntegrationWebhookInfoResult> GetWebhookInfoAsync(
        BotIntegration integration,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var url = integration.ExternalId?.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            return Task.FromResult(
                new IntegrationWebhookInfoResult(
                    false,
                    IntegrationWebhookFailureCategory.BadRequestGuard,
                    "Webhook callback URL is missing.",
                    null,
                    null,
                    null));
        }

        var info = new IntegrationWebhookInfoData(url, 0, null, null, false, null);
        return Task.FromResult(
            new IntegrationWebhookInfoResult(true, IntegrationWebhookFailureCategory.None, null, null, null, info));
    }

    /// <inheritdoc />
    public async Task<IntegrationWebhookDeleteResult> DeleteWebhookAsync(
        BotIntegration integration,
        bool dropPendingUpdates,
        CancellationToken cancellationToken)
    {
        _ = integration;
        _ = dropPendingUpdates;

        if (string.IsNullOrWhiteSpace(_settings.AccessToken) || string.IsNullOrWhiteSpace(_settings.PhoneNumberId))
        {
            return new IntegrationWebhookDeleteResult(
                true,
                IntegrationWebhookFailureCategory.None,
                null,
                null,
                null);
        }

        try
        {
            var requestUrl = $"{GraphApiBaseUrl}/{_settings.PhoneNumberId.Trim()}/subscribed_apps";
            var client = _httpClientFactory.CreateClient("WhatsApp");
            using var request = new HttpRequestMessage(HttpMethod.Delete, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.AccessToken.Trim());

            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "whatsapp_unsubscribe_failed | status={Status} body={Body}",
                    (int)response.StatusCode,
                    body);

                return new IntegrationWebhookDeleteResult(
                    false,
                    IntegrationWebhookFailureCategory.UpstreamProviderError,
                    "Failed to unsubscribe WhatsApp app.",
                    (int)response.StatusCode,
                    body);
            }

            return new IntegrationWebhookDeleteResult(true, IntegrationWebhookFailureCategory.None, null, null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "whatsapp_webhook_delete_error");
            return new IntegrationWebhookDeleteResult(
                false,
                IntegrationWebhookFailureCategory.InternalError,
                "Unexpected error while deleting WhatsApp subscription.",
                null,
                ex.Message);
        }
    }
}
