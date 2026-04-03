namespace AiEmployee.Infrastructure.Telegram;

public interface ITelegramClient
{
    Task SendMessageAsync(long chatId, string text);
}
