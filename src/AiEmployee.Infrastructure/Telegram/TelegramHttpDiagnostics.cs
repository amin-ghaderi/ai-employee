namespace AiEmployee.Infrastructure.Telegram;

/// <summary>Result of calling Telegram Bot API <c>getMe</c> and <c>getWebhookInfo</c> using the configured token.</summary>
public sealed class TelegramHttpDiagnostics
{
    public string MaskedToken { get; set; } = string.Empty;

    public bool GetMeOk { get; set; }
    public string? GetMeUsername { get; set; }
    public long? GetMeBotUserId { get; set; }
    public string? GetMeError { get; set; }

    public bool WebhookInfoOk { get; set; }
    public string? WebhookUrl { get; set; }
    public int? WebhookPendingUpdateCount { get; set; }
    public string? WebhookLastErrorMessage { get; set; }
    public string? WebhookInfoError { get; set; }
}
