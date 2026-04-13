namespace AiEmployee.Application.Messaging;

public static class IncomingMessageMetadataKeys
{
    public const string IntegrationExternalId = "integrationExternalId";
    /// <summary>Telegram <c>update_id</c> as a decimal string.</summary>
    public const string TelegramUpdateId = "telegramUpdateId";
    /// <summary>Stable per-bot scope for deduplication (integration route id or hash of bot token).</summary>
    public const string TelegramBotScopeKey = "telegramBotScopeKey";
    public const string Username = "username";
    public const string FirstName = "firstName";
    public const string LastName = "lastName";
}
