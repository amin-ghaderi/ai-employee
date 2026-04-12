namespace AiEmployee.Application.Messaging;

public interface IChannelAdapter
{
    /// <summary>
    /// Maps a channel-specific deserialized payload to <see cref="IncomingMessage"/>.
    /// Returns <c>null</c> when the request should be ignored (no handler invocation).
    /// </summary>
    /// <param name="telegramIntegrationId">When set (multi-bot), selects that BotIntegrations row for routing metadata; otherwise resolution uses configuration or a single enabled integration.</param>
    Task<IncomingMessage?> MapAsync(
        object? rawRequest,
        Guid? telegramIntegrationId = null,
        CancellationToken cancellationToken = default);
}
