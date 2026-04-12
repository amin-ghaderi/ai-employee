using System.Text.Json;
using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Options;
using AiEmployee.Application.Telegram;
using AiEmployee.Domain.BotConfiguration;
using Microsoft.Extensions.Logging;

namespace AiEmployee.Application.Integrations;

public sealed class TelegramWebhookApplicationService : ITelegramWebhookApplicationService
{
    private readonly IBotIntegrationRepository _integrations;
    private readonly IPublicBaseUrlProvider _publicBaseUrl;
    private readonly ITelegramWebhookApiClient _telegram;
    private readonly ILogger<TelegramWebhookApplicationService> _logger;

    public TelegramWebhookApplicationService(
        IBotIntegrationRepository integrations,
        IPublicBaseUrlProvider publicBaseUrl,
        ITelegramWebhookApiClient telegram,
        ILogger<TelegramWebhookApplicationService> logger)
    {
        _integrations = integrations;
        _publicBaseUrl = publicBaseUrl;
        _telegram = telegram;
        _logger = logger;
    }

    public async Task<TelegramWebhookSyncResult> SyncWebhookAsync(Guid integrationId, CancellationToken cancellationToken = default)
    {
        var integration = await _integrations.GetByIdAsync(integrationId, cancellationToken).ConfigureAwait(false);
        var tokenOutcome = ValidateTelegramIntegration(integration, integrationId);
        if (tokenOutcome is not null)
            return tokenOutcome;

        var token = integration!.ExternalId.Trim();
        var masked = TelegramTokenMasking.MaskBotToken(token);
        _logger.LogInformation("webhook_sync_start | integrationId={IntegrationId} token={Masked}", integrationId, masked);

        string? publicBase;
        try
        {
            publicBase = _publicBaseUrl.GetPublicBaseUrl();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "webhook_sync_invalid_public_base_url | integrationId={IntegrationId}", integrationId);
            return TelegramWebhookSyncResult.Failed(ex.Message, configuredWebhookUrl: null);
        }

        if (string.IsNullOrEmpty(publicBase))
        {
            return TelegramWebhookSyncResult.Failed(
                "App:PublicBaseUrl is not configured. Set it to your public HTTPS origin (e.g. ngrok or production domain).",
                configuredWebhookUrl: null);
        }

        var webhookUrl = $"{publicBase}/api/telegram/webhook/{integrationId}";
        var raw = await _telegram.SetWebhookAsync(token, webhookUrl, dropPendingUpdates: false, cancellationToken)
            .ConfigureAwait(false);

        if (!raw.HttpSuccess)
        {
            return TelegramWebhookSyncResult.Failed(
                raw.Description ?? "Telegram HTTP request failed.",
                raw.ErrorCode,
                raw.Description,
                webhookUrl);
        }

        if (!raw.TelegramOk)
        {
            return TelegramWebhookSyncResult.Failed(
                raw.Description ?? "Telegram API returned ok=false.",
                raw.ErrorCode,
                raw.Description,
                webhookUrl);
        }

        if (!TryGetResultBoolean(raw.ResponseBody, out var resultOk) || !resultOk)
        {
            return TelegramWebhookSyncResult.Failed(
                "Telegram setWebhook did not return result=true.",
                raw.ErrorCode,
                raw.Description,
                webhookUrl);
        }

        _logger.LogInformation(
            "webhook_sync_success | integrationId={IntegrationId} token={Masked} webhookUrlLength={Len}",
            integrationId,
            masked,
            webhookUrl.Length);

        return TelegramWebhookSyncResult.Ok(webhookUrl, raw.Description);
    }

    public async Task<TelegramWebhookInfoResult> GetWebhookStatusAsync(
        Guid integrationId,
        CancellationToken cancellationToken = default)
    {
        var integration = await _integrations.GetByIdAsync(integrationId, cancellationToken).ConfigureAwait(false);
        var fail = ValidateTelegramIntegration(integration, integrationId);
        if (fail is not null)
        {
            return new TelegramWebhookInfoResult(
                false,
                fail.Message,
                fail.TelegramErrorCode,
                fail.TelegramDescription,
                null);
        }

        var token = integration!.ExternalId.Trim();
        _logger.LogInformation(
            "webhook_info_start | integrationId={IntegrationId} token={Masked}",
            integrationId,
            TelegramTokenMasking.MaskBotToken(token));

        var raw = await _telegram.GetWebhookInfoAsync(token, cancellationToken).ConfigureAwait(false);

        if (!raw.HttpSuccess)
            return new TelegramWebhookInfoResult(false, raw.Description ?? "HTTP error.", raw.ErrorCode, raw.Description, null);

        if (!raw.TelegramOk)
            return new TelegramWebhookInfoResult(false, raw.Description ?? "Telegram ok=false.", raw.ErrorCode, raw.Description, null);

        var info = TryParseWebhookInfo(raw.ResponseBody);
        return new TelegramWebhookInfoResult(true, null, null, raw.Description, info);
    }

    public async Task<TelegramWebhookDeleteResult> DeleteWebhookAsync(
        Guid integrationId,
        bool dropPendingUpdates = false,
        CancellationToken cancellationToken = default)
    {
        var integration = await _integrations.GetByIdAsync(integrationId, cancellationToken).ConfigureAwait(false);
        var fail = ValidateTelegramIntegration(integration, integrationId);
        if (fail is not null)
        {
            return new TelegramWebhookDeleteResult(false, fail.Message, fail.TelegramErrorCode, fail.TelegramDescription);
        }

        var token = integration!.ExternalId.Trim();
        _logger.LogInformation(
            "webhook_delete_start | integrationId={IntegrationId} token={Masked} dropPending={Drop}",
            integrationId,
            TelegramTokenMasking.MaskBotToken(token),
            dropPendingUpdates);

        var raw = await _telegram.DeleteWebhookAsync(token, dropPendingUpdates, cancellationToken).ConfigureAwait(false);

        if (!raw.HttpSuccess)
            return new TelegramWebhookDeleteResult(false, raw.Description ?? "HTTP error.", raw.ErrorCode, raw.Description);

        if (!raw.TelegramOk)
            return new TelegramWebhookDeleteResult(false, raw.Description ?? "Telegram ok=false.", raw.ErrorCode, raw.Description);

        if (!TryGetResultBoolean(raw.ResponseBody, out var ok) || !ok)
            return new TelegramWebhookDeleteResult(false, "Telegram deleteWebhook did not return result=true.", raw.ErrorCode, raw.Description);

        _logger.LogInformation("webhook_delete_success | integrationId={IntegrationId}", integrationId);
        return new TelegramWebhookDeleteResult(true, null, null, raw.Description);
    }

    private static TelegramWebhookSyncResult? ValidateTelegramIntegration(BotIntegration? integration, Guid integrationId)
    {
        if (integration is null)
            return TelegramWebhookSyncResult.Failed($"No integration found for id '{integrationId}'.", configuredWebhookUrl: null);

        if (!BotIntegrationChannelNames.IsTelegramChannel(integration.Channel))
            return TelegramWebhookSyncResult.Failed("Integration is not a Telegram integration.", configuredWebhookUrl: null);

        if (string.IsNullOrWhiteSpace(integration.ExternalId))
            return TelegramWebhookSyncResult.Failed("Telegram bot token (ExternalId) is missing.", configuredWebhookUrl: null);

        return null;
    }

    private static bool TryGetResultBoolean(string? json, out bool value)
    {
        value = false;
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("result", out var r))
                return false;
            if (r.ValueKind == JsonValueKind.True)
            {
                value = true;
                return true;
            }

            if (r.ValueKind == JsonValueKind.False)
                return true;

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static TelegramWebhookInfoData? TryParseWebhookInfo(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("result", out var r) || r.ValueKind != JsonValueKind.Object)
                return null;

            string? url = r.TryGetProperty("url", out var u) && u.ValueKind == JsonValueKind.String ? u.GetString() : null;
            int? pending = null;
            if (r.TryGetProperty("pending_update_count", out var p) && p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var pc))
                pending = pc;

            string? lastErr = r.TryGetProperty("last_error_message", out var le) && le.ValueKind == JsonValueKind.String
                ? le.GetString()
                : null;

            long? lastErrDate = null;
            if (r.TryGetProperty("last_error_date", out var led) && led.ValueKind == JsonValueKind.Number && led.TryGetInt64(out var dt))
                lastErrDate = dt;

            bool? hasCert = null;
            if (r.TryGetProperty("has_custom_certificate", out var hc) && hc.ValueKind is JsonValueKind.True or JsonValueKind.False)
                hasCert = hc.GetBoolean();

            int? maxConn = null;
            if (r.TryGetProperty("max_connections", out var mc) && mc.ValueKind == JsonValueKind.Number && mc.TryGetInt32(out var mx))
                maxConn = mx;

            return new TelegramWebhookInfoData(url, pending, lastErr, lastErrDate, hasCert, maxConn);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
