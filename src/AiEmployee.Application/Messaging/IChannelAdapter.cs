namespace AiEmployee.Application.Messaging;

public interface IChannelAdapter
{
    /// <summary>
    /// Maps a channel-specific deserialized payload to <see cref="IncomingMessage"/>.
    /// Returns <c>null</c> when the request should be ignored (no handler invocation).
    /// </summary>
    IncomingMessage? Map(object? rawRequest);
}
