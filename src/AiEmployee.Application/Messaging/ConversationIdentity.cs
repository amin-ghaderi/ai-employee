using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Application.Messaging;

/// <summary>
/// Derives the persisted conversation primary key from channel-specific identifiers.
/// Telegram <see cref="IncomingMessage.ExternalChatId"/> is only the Telegram chat id; it is shared
/// across all bots talking to the same user (private) or the same group, so it must not be used alone as <c>Conversations.Id</c>.
/// </summary>
public static class ConversationIdentity
{
    /// <summary>Separates segments; not used in Telegram chat ids or scope keys.</summary>
    private const char SegmentSeparator = '|';

    /// <summary>
    /// Builds <c>Conversations.Id</c> / <c>Message.ConversationId</c> for an incoming message.
    /// </summary>
    public static string ResolveConversationId(IncomingMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!BotIntegrationChannelNames.IsTelegramChannel(message.Channel))
            return message.ExternalChatId;

        var scope = ReadMetadata(message.Metadata, IncomingMessageMetadataKeys.TelegramBotScopeKey);
        if (string.IsNullOrWhiteSpace(scope))
        {
            // Extremely old callers/tests without scope: preserve previous behavior (single-bot only).
            return message.ExternalChatId;
        }

        return BuildTelegramConversationId(scope.Trim(), message.ExternalChatId);
    }

    /// <summary>
    /// Builds the persisted conversation id for Telegram when the bot scope key is already known (e.g. admin real-flow reset).
    /// </summary>
    public static string ResolveTelegramConversationId(string telegramBotScopeKey, string externalChatId)
    {
        if (string.IsNullOrWhiteSpace(telegramBotScopeKey))
            return externalChatId;
        return BuildTelegramConversationId(telegramBotScopeKey.Trim(), externalChatId);
    }

    private static string BuildTelegramConversationId(string telegramBotScopeKey, string externalChatId)
    {
        if (string.IsNullOrWhiteSpace(externalChatId))
            throw new ArgumentException("External chat id is required.", nameof(externalChatId));

        return string.Concat(
            "telegram",
            SegmentSeparator,
            telegramBotScopeKey,
            SegmentSeparator,
            externalChatId);
    }

    private static string? ReadMetadata(IReadOnlyDictionary<string, string>? metadata, string key) =>
        metadata is not null && metadata.TryGetValue(key, out var v) ? v : null;
}
