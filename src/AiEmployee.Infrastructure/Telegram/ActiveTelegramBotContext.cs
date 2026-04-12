using AiEmployee.Application.Telegram;

namespace AiEmployee.Infrastructure.Telegram;

public sealed class ActiveTelegramBotContext : IActiveTelegramBotContext
{
    public string? Token { get; set; }
}
