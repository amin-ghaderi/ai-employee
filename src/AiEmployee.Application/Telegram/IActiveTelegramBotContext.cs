namespace AiEmployee.Application.Telegram;

/// <summary>
/// Per-request Telegram bot API token resolved from JudgeBotConfiguration / database.
/// </summary>
public interface IActiveTelegramBotContext
{
    string? Token { get; set; }
}
