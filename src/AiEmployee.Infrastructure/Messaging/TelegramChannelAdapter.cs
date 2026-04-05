using AiEmployee.Application.Messaging;
using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Infrastructure.Telegram;
using Microsoft.Extensions.Options;

namespace AiEmployee.Infrastructure.Messaging;

public sealed class TelegramChannelAdapter : IChannelAdapter
{
    private readonly IOptions<TelegramSettings> _telegramSettings;

    public TelegramChannelAdapter(IOptions<TelegramSettings> telegramSettings)
    {
        _telegramSettings = telegramSettings;
    }

    public IncomingMessage? Map(object? rawRequest)
    {
        if (rawRequest is not TelegramUpdate update)
            return null;

        if (update.Message?.Chat is null || string.IsNullOrWhiteSpace(update.Message.Text))
            return null;

        var externalChatId = update.Message.Chat.Id.ToString();
        var externalUserId = update.Message.From?.Id.ToString() ?? externalChatId;
        var from = update.Message.From;

        var metadata = new Dictionary<string, string>
        {
            [IncomingMessageMetadataKeys.IntegrationExternalId] = _telegramSettings.Value.BotToken,
        };
        if (!string.IsNullOrEmpty(from?.Username))
            metadata[IncomingMessageMetadataKeys.Username] = from.Username;
        if (!string.IsNullOrEmpty(from?.FirstName))
            metadata[IncomingMessageMetadataKeys.FirstName] = from.FirstName;
        if (!string.IsNullOrEmpty(from?.LastName))
            metadata[IncomingMessageMetadataKeys.LastName] = from.LastName;

        return new IncomingMessage(
            BotIntegrationChannelNames.Telegram,
            externalUserId,
            externalChatId,
            update.Message.Text.Trim(),
            metadata);
    }
}
