namespace AiEmployee.Application.Integrations;

/// <summary>Low-level Telegram Bot API calls for webhook lifecycle (token passed per call).</summary>
public interface ITelegramWebhookApiClient
{
    /// <summary>POST <c>setWebhook</c> for the given bot token.</summary>
    Task<TelegramWebhookMethodRawResult> SetWebhookAsync(
        string botToken,
        string webhookUrl,
        bool dropPendingUpdates,
        CancellationToken cancellationToken = default);

    /// <summary>GET <c>getWebhookInfo</c> for the given bot token.</summary>
    Task<TelegramWebhookMethodRawResult> GetWebhookInfoAsync(
        string botToken,
        CancellationToken cancellationToken = default);

    /// <summary>POST <c>deleteWebhook</c> for the given bot token.</summary>
    Task<TelegramWebhookMethodRawResult> DeleteWebhookAsync(
        string botToken,
        bool dropPendingUpdates,
        CancellationToken cancellationToken = default);
}

/// <summary>Raw Telegram JSON envelope (Telegram often returns HTTP 200 with <c>ok:false</c>). <see cref="ResponseBody"/> is the full response text for further parsing.</summary>
public sealed record TelegramWebhookMethodRawResult(
    bool HttpSuccess,
    int HttpStatusCode,
    bool TelegramOk,
    int? ErrorCode,
    string? Description,
    string? ResponseBody);
