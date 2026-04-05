using AiEmployee.Application.Messaging;
using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Infrastructure.Telegram;

namespace AiEmployee.Infrastructure.Messaging;

public sealed class TelegramMessageSender : IChannelMessageSender
{
    private readonly ITelegramClient _telegramClient;

    public TelegramMessageSender(ITelegramClient telegramClient)
    {
        _telegramClient = telegramClient;
    }

    public string Channel => BotIntegrationChannelNames.Telegram;

    public async Task SendAsync(string externalChatId, string text)
    {
        if (!long.TryParse(externalChatId, out var chatId))
            return;

        await _telegramClient.SendMessageAsync(chatId, text);
    }
}
