using System.Net.Http.Json;
using System.Text.Json;
using AiEmployee.Application.Telegram;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiEmployee.Infrastructure.Telegram;

public class TelegramClient : ITelegramClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<TelegramSettings> _options;
    private readonly IActiveTelegramBotContext _activeToken;
    private readonly ILogger<TelegramClient> _logger;

    public TelegramClient(
        HttpClient httpClient,
        IOptions<TelegramSettings> options,
        IActiveTelegramBotContext activeToken,
        ILogger<TelegramClient> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _activeToken = activeToken;
        _logger = logger;
    }

    public async Task SendMessageAsync(long chatId, string text)
    {
        // Telegram uses negative chat_id for groups and supergroups (e.g. -100…); only DMs are positive.
        if (chatId == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chatId), "chatId cannot be zero.");
        }

        _logger.LogInformation("Sending to chatId: {ChatId}", chatId);

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("text cannot be empty.", nameof(text));
        }

        var token = _activeToken.Token?.Trim();
        if (string.IsNullOrWhiteSpace(token))
            token = _options.Value.BotToken?.Trim();

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                "No Telegram bot token is available. Resolve a bot configuration with BotIntegrations.ExternalId or set Telegram:BotToken as a fallback.");
        }

        var url = $"https://api.telegram.org/bot{token}/sendMessage";

        _logger.LogInformation(
            "telegram_sendMessage_start | token={MaskedToken} chatId={ChatId} textLength={TextLength} textPreview={TextPreview}",
            TelegramTokenUtilities.MaskBotToken(token),
            chatId,
            text.Length,
            text.Length <= 120 ? text : text[..120] + "…");

        var response = await _httpClient.PostAsJsonAsync(url, new { chat_id = chatId, text });

        var responseBody = await response.Content.ReadAsStringAsync();
        _logger.LogInformation(
            "telegram_sendMessage_done | chatId={ChatId} statusCode={StatusCode} body={Body}",
            chatId,
            (int)response.StatusCode,
            responseBody.Length > 2000 ? responseBody[..2000] + "…" : responseBody);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Telegram sendMessage failed: {(int)response.StatusCode} {response.ReasonPhrase}. Response: {responseBody}");
        }
    }

    public async Task<TelegramHttpDiagnostics> FetchDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        var token = _options.Value.BotToken?.Trim() ?? string.Empty;
        var masked = TelegramTokenUtilities.MaskBotToken(token);
        var diag = new TelegramHttpDiagnostics { MaskedToken = masked };

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Telegram diagnostics: BotToken is missing.");
            diag.GetMeError = "BotToken is not configured.";
            return diag;
        }

        _logger.LogInformation(
            "Telegram diagnostics: calling getMe/getWebhookInfo with token {Masked}",
            masked);

        try
        {
            var getMeUrl = $"https://api.telegram.org/bot{token}/getMe";
            using var getMeResponse = await _httpClient.GetAsync(getMeUrl, cancellationToken).ConfigureAwait(false);
            var getMeBody = await getMeResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            ParseGetMe(getMeBody, diag);
        }
        catch (Exception ex)
        {
            diag.GetMeOk = false;
            diag.GetMeError = ex.Message;
        }

        try
        {
            var hookUrl = $"https://api.telegram.org/bot{token}/getWebhookInfo";
            using var hookResponse = await _httpClient.GetAsync(hookUrl, cancellationToken).ConfigureAwait(false);
            var hookBody = await hookResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            ParseWebhookInfo(hookBody, diag);
        }
        catch (Exception ex)
        {
            diag.WebhookInfoOk = false;
            diag.WebhookInfoError = ex.Message;
        }

        return diag;
    }

    private static void ParseGetMe(string json, TelegramHttpDiagnostics d)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var ok = root.TryGetProperty("ok", out var okEl) && okEl.ValueKind == JsonValueKind.True;
            if (!ok)
            {
                d.GetMeError = TryTelegramDescription(root) ?? Truncate(json, 500);
                return;
            }

            if (!root.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Object)
            {
                d.GetMeError = Truncate(json, 500);
                return;
            }

            d.GetMeOk = true;
            if (result.TryGetProperty("username", out var un) && un.ValueKind == JsonValueKind.String)
                d.GetMeUsername = un.GetString();
            if (result.TryGetProperty("id", out var id) && id.TryGetInt64(out var lid))
                d.GetMeBotUserId = lid;
        }
        catch (JsonException)
        {
            d.GetMeError = "Invalid JSON from getMe.";
        }
    }

    private static void ParseWebhookInfo(string json, TelegramHttpDiagnostics d)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var ok = root.TryGetProperty("ok", out var okEl) && okEl.ValueKind == JsonValueKind.True;
            if (!ok)
            {
                d.WebhookInfoError = TryTelegramDescription(root) ?? Truncate(json, 500);
                return;
            }

            if (!root.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Object)
            {
                d.WebhookInfoError = Truncate(json, 500);
                return;
            }

            d.WebhookInfoOk = true;
            if (result.TryGetProperty("url", out var url) && url.ValueKind == JsonValueKind.String)
                d.WebhookUrl = url.GetString();
            if (result.TryGetProperty("pending_update_count", out var p) && p.TryGetInt32(out var pc))
                d.WebhookPendingUpdateCount = pc;
            if (result.TryGetProperty("last_error_message", out var lem) && lem.ValueKind == JsonValueKind.String)
                d.WebhookLastErrorMessage = lem.GetString();
        }
        catch (JsonException)
        {
            d.WebhookInfoError = "Invalid JSON from getWebhookInfo.";
        }
    }

    private static string? TryTelegramDescription(JsonElement root) =>
        root.TryGetProperty("description", out var d) && d.ValueKind == JsonValueKind.String
            ? d.GetString()
            : null;

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";
}
