using AiEmployee.Application.Messaging;
using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Infrastructure.Integrations.Slack;

/// <summary>Maps Slack Events API payloads to the platform <see cref="IncomingMessage"/> shape.</summary>
public static class SlackEventMapper
{
    /// <summary>
    /// Builds <see cref="IncomingMessage"/> for assistant processing, or <c>null</c> when the event should be ignored.
    /// <see cref="IncomingMessage.Metadata"/> includes <see cref="IncomingMessageMetadataKeys.IntegrationExternalId"/>
    /// (the integration row's <see cref="BotIntegration.ExternalId"/>) for bot resolution.
    /// </summary>
    public static IncomingMessage? MapToIncomingMessage(SlackEventRequest request, BotIntegration integration)
    {
        if (request.Event is null)
            return null;

        if (!string.Equals(request.Event.Type, "message", StringComparison.Ordinal))
            return null;

        if (!string.IsNullOrEmpty(request.Event.BotId))
            return null;

        if (string.Equals(request.Event.Subtype, "bot_message", StringComparison.Ordinal))
            return null;

        if (!string.IsNullOrEmpty(request.Event.Subtype)
            && !string.Equals(request.Event.Subtype, "thread_broadcast", StringComparison.Ordinal))
        {
            // file_share, message_changed, etc.
            return null;
        }

        var text = request.Event.Text?.Trim();
        if (string.IsNullOrEmpty(text))
            return null;

        var channelId = request.Event.Channel?.Trim();
        if (string.IsNullOrEmpty(channelId))
            return null;

        var userId = request.Event.User?.Trim();
        if (string.IsNullOrEmpty(userId))
            return null;

        var normalizedChannel = BotIntegrationChannelNames.NormalizeChannelValue(integration.Channel);
        var externalChatId = BuildSlackConversationExternalId(channelId, request.Event.ThreadTs);
        var externalId = integration.ExternalId.Trim();

        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [IncomingMessageMetadataKeys.IntegrationExternalId] = externalId,
        };

        return new IncomingMessage(
            normalizedChannel,
            userId,
            externalChatId,
            text,
            metadata);
    }

    /// <summary>
    /// Slack replies must include <c>thread_ts</c>; we encode the thread root in <see cref="IncomingMessage.ExternalChatId"/>
    /// as <c>{channel}|{thread_ts}</c> when the message belongs to a thread.
    /// </summary>
    public static string BuildSlackConversationExternalId(string channelId, string? threadTs)
    {
        if (string.IsNullOrWhiteSpace(threadTs))
            return channelId;
        return $"{channelId}|{threadTs.Trim()}";
    }
}
