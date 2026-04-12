using AiEmployee.Domain.BotConfiguration;
using Microsoft.Extensions.Logging;

namespace AiEmployee.Application.Messaging;

public sealed class OutgoingMessageDispatcher : IOutgoingMessageClient
{
    private readonly IReadOnlyDictionary<string, IChannelMessageSender> _sendersByChannel;
    private readonly ILogger<OutgoingMessageDispatcher> _logger;

    public OutgoingMessageDispatcher(
        IEnumerable<IChannelMessageSender> senders,
        ILogger<OutgoingMessageDispatcher> logger)
    {
        _logger = logger;
        var map = new Dictionary<string, IChannelMessageSender>(StringComparer.OrdinalIgnoreCase);
        foreach (var sender in senders)
        {
            if (!map.TryAdd(sender.Channel, sender))
                throw new InvalidOperationException(
                    $"Duplicate {nameof(IChannelMessageSender)} registration for channel '{sender.Channel}'.");
        }

        _sendersByChannel = map;
    }

    public async Task SendMessageAsync(string channel, string externalChatId, string text)
    {
        if (string.IsNullOrWhiteSpace(channel))
        {
            _logger.LogWarning("Outgoing message skipped: channel is empty.");
            return;
        }

        var key = channel.Trim();
        if (!_sendersByChannel.TryGetValue(key, out var sender))
        {
            var providerKey = IntegrationProviders.TryResolveFromChannel(key);
            if (providerKey is not null && _sendersByChannel.TryGetValue(providerKey, out var byProvider))
                sender = byProvider;
        }

        if (sender is null)
        {
            _logger.LogWarning("No outgoing sender registered for channel {Channel}", key);
            return;
        }

        _logger.LogInformation(
            "Outgoing message | channel={Channel} chatId={ChatId} textChars={TextChars}",
            key,
            externalChatId,
            text.Length);

        await sender.SendAsync(externalChatId, text);
    }
}
