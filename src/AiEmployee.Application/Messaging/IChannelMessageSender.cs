namespace AiEmployee.Application.Messaging;

/// <summary>
/// Sends outbound messages for a single channel implementation.
/// Routing by channel name is handled by <see cref="OutgoingMessageDispatcher"/>.
/// </summary>
public interface IChannelMessageSender
{
    /// <summary>Canonical channel identifier this sender handles.</summary>
    string Channel { get; }

    Task SendAsync(string externalChatId, string text);
}
