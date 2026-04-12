using System.Net.Http.Json;
using System.Text.Json;
using AiEmployee.Application.Integrations;
using Microsoft.Extensions.Logging;

namespace AiEmployee.Infrastructure.Telegram;

public sealed class TelegramWebhookApiClient : ITelegramWebhookApiClient
{
    private const int MaxLoggedBodyChars = 2000;
    private const int MaxStoredBodyChars = 262144;

    private readonly HttpClient _httpClient;
    private readonly ILogger<TelegramWebhookApiClient> _logger;

    public TelegramWebhookApiClient(HttpClient httpClient, ILogger<TelegramWebhookApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<TelegramWebhookMethodRawResult> SetWebhookAsync(
        string botToken,
        string webhookUrl,
        bool dropPendingUpdates,
        CancellationToken cancellationToken = default)
    {
        var masked = TelegramTokenUtilities.MaskBotToken(botToken);
        _logger.LogInformation(
            "telegram_setWebhook_request | token={MaskedToken} urlLength={UrlLength}",
            masked,
            webhookUrl.Length);

        return await PostJsonAsync(
                botToken,
                "setWebhook",
                new { url = webhookUrl, drop_pending_updates = dropPendingUpdates },
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TelegramWebhookMethodRawResult> GetWebhookInfoAsync(
        string botToken,
        CancellationToken cancellationToken = default)
    {
        var masked = TelegramTokenUtilities.MaskBotToken(botToken);
        _logger.LogInformation("telegram_getWebhookInfo_request | token={MaskedToken}", masked);

        return await GetAsync(botToken, "getWebhookInfo", cancellationToken).ConfigureAwait(false);
    }

    public async Task<TelegramWebhookMethodRawResult> DeleteWebhookAsync(
        string botToken,
        bool dropPendingUpdates,
        CancellationToken cancellationToken = default)
    {
        var masked = TelegramTokenUtilities.MaskBotToken(botToken);
        _logger.LogInformation(
            "telegram_deleteWebhook_request | token={MaskedToken} dropPending={Drop}",
            masked,
            dropPendingUpdates);

        var query = dropPendingUpdates ? "?drop_pending_updates=true" : string.Empty;
        return await PostEmptyAsync(botToken, $"deleteWebhook{query}", cancellationToken).ConfigureAwait(false);
    }

    private async Task<TelegramWebhookMethodRawResult> PostJsonAsync(
        string botToken,
        string method,
        object payload,
        CancellationToken cancellationToken)
    {
        var url = $"https://api.telegram.org/bot{botToken}/{method}";
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken)
                .ConfigureAwait(false);
            return await ReadTelegramResponseAsync(response, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new TelegramWebhookMethodRawResult(false, 0, false, null, "Request timed out.", null);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "telegram_{Method}_http_error | token={Masked}", method, TelegramTokenUtilities.MaskBotToken(botToken));
            return new TelegramWebhookMethodRawResult(false, 0, false, null, ex.Message, null);
        }
    }

    private async Task<TelegramWebhookMethodRawResult> GetAsync(
        string botToken,
        string method,
        CancellationToken cancellationToken)
    {
        var url = $"https://api.telegram.org/bot{botToken}/{method}";
        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            return await ReadTelegramResponseAsync(response, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new TelegramWebhookMethodRawResult(false, 0, false, null, "Request timed out.", null);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "telegram_{Method}_http_error | token={Masked}", method, TelegramTokenUtilities.MaskBotToken(botToken));
            return new TelegramWebhookMethodRawResult(false, 0, false, null, ex.Message, null);
        }
    }

    private async Task<TelegramWebhookMethodRawResult> PostEmptyAsync(
        string botToken,
        string methodAndQuery,
        CancellationToken cancellationToken)
    {
        var url = $"https://api.telegram.org/bot{botToken}/{methodAndQuery}";
        try
        {
            using var response = await _httpClient.PostAsync(url, null, cancellationToken).ConfigureAwait(false);
            return await ReadTelegramResponseAsync(response, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new TelegramWebhookMethodRawResult(false, 0, false, null, "Request timed out.", null);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                ex,
                "telegram_deleteWebhook_http_error | token={Masked}",
                TelegramTokenUtilities.MaskBotToken(botToken));
            return new TelegramWebhookMethodRawResult(false, 0, false, null, ex.Message, null);
        }
    }

    private async Task<TelegramWebhookMethodRawResult> ReadTelegramResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var status = (int)response.StatusCode;
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var stored = body.Length <= MaxStoredBodyChars ? body : body[..MaxStoredBodyChars] + "…[truncated]";
        var logPreview = body.Length <= MaxLoggedBodyChars ? body : body[..MaxLoggedBodyChars] + "…";

        _logger.LogInformation(
            "telegram_response | httpStatus={HttpStatus} bodyPreview={BodyPreview}",
            status,
            logPreview);

        if (!response.IsSuccessStatusCode)
            return new TelegramWebhookMethodRawResult(false, status, false, null, $"HTTP {status}: {response.ReasonPhrase}", stored);

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var ok = root.TryGetProperty("ok", out var okEl) && okEl.ValueKind == JsonValueKind.True;
            int? errCode = null;
            if (root.TryGetProperty("error_code", out var ec) && ec.ValueKind == JsonValueKind.Number && ec.TryGetInt32(out var code))
                errCode = code;

            string? description = null;
            if (root.TryGetProperty("description", out var desc) && desc.ValueKind == JsonValueKind.String)
                description = desc.GetString();

            return new TelegramWebhookMethodRawResult(true, status, ok, errCode, description, stored);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "telegram_response_invalid_json");
            return new TelegramWebhookMethodRawResult(true, status, false, null, "Invalid JSON from Telegram.", stored);
        }
    }
}
