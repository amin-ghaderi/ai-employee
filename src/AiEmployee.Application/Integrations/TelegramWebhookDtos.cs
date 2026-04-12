namespace AiEmployee.Application.Integrations;

/// <summary>Outcome of calling Telegram <c>setWebhook</c> for an integration.</summary>
public sealed record TelegramWebhookSyncResult(
    bool Success,
    string? Message,
    int? TelegramErrorCode,
    string? TelegramDescription,
    string? ConfiguredWebhookUrl)
{
    public static TelegramWebhookSyncResult Ok(string configuredWebhookUrl, string? telegramDescription) =>
        new(true, null, null, telegramDescription, configuredWebhookUrl);

    public static TelegramWebhookSyncResult Failed(
        string message,
        int? telegramErrorCode = null,
        string? telegramDescription = null,
        string? configuredWebhookUrl = null) =>
        new(false, message, telegramErrorCode, telegramDescription, configuredWebhookUrl);
}

/// <summary>Telegram <c>getWebhookInfo</c> fields relevant for diagnostics.</summary>
public sealed record TelegramWebhookInfoData(
    string? Url,
    int? PendingUpdateCount,
    string? LastErrorMessage,
    long? LastErrorDate,
    bool? HasCustomCertificate,
    int? MaxConnections);

/// <summary>Outcome of <c>getWebhookInfo</c> for an integration.</summary>
public sealed record TelegramWebhookInfoResult(
    bool Success,
    string? Message,
    int? TelegramErrorCode,
    string? TelegramDescription,
    TelegramWebhookInfoData? Info);

/// <summary>Outcome of <c>deleteWebhook</c> for an integration.</summary>
public sealed record TelegramWebhookDeleteResult(
    bool Success,
    string? Message,
    int? TelegramErrorCode,
    string? TelegramDescription);
