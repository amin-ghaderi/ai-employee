namespace AiEmployee.Infrastructure.Telegram;

public class TelegramSettings
{
    /// <summary>Optional legacy/bootstrap token (diagnostics, seeder, or single-bot ambiguity). Runtime sends prefer scoped IActiveTelegramBotContext.</summary>
    public string? BotToken { get; set; }
}
