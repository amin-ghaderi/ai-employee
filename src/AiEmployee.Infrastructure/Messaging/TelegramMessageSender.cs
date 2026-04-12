using AiEmployee.Application.Messaging;
using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Infrastructure.Telegram;
using Microsoft.Extensions.Logging;

namespace AiEmployee.Infrastructure.Messaging;

public sealed class TelegramMessageSender : IChannelMessageSender
{
    private readonly ITelegramClient _telegramClient;
    private readonly ILogger<TelegramMessageSender> _logger;

    public TelegramMessageSender(
        ITelegramClient telegramClient,
        ILogger<TelegramMessageSender> logger)
    {
        _telegramClient = telegramClient;
        _logger = logger;
    }

    public string Channel => BotIntegrationChannelNames.Telegram;

    public async Task SendAsync(string externalChatId, string text)
    {
        var raw = externalChatId?.Trim();
        if (string.IsNullOrEmpty(raw))
        {
            _logger.LogWarning(
                "Telegram send skipped: externalChatId is null or empty (channel={Channel}).",
                Channel);
            return;
        }

        if (!long.TryParse(raw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var chatId))
        {
            _logger.LogWarning(
                "Telegram send skipped: externalChatId is not a valid Telegram chat id (channel={Channel}, length={Length}, preview={Preview}).",
                Channel,
                raw.Length,
                raw.Length <= 32 ? raw : raw[..32] + "…");
            return;
        }

        await _telegramClient.SendMessageAsync(chatId, text);
    }
}
