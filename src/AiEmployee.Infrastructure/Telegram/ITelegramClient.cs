namespace AiEmployee.Infrastructure.Telegram;

public interface ITelegramClient
{
    Task SendMessageAsync(long chatId, string text);

    /// <summary>Calls <c>getMe</c> and <c>getWebhookInfo</c> for admin diagnostics (masked token in result only).</summary>
    Task<TelegramHttpDiagnostics> FetchDiagnosticsAsync(CancellationToken cancellationToken = default);
}
