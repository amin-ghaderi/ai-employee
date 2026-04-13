namespace AiEmployee.Domain.Entities;

/// <summary>
/// Records a processed Telegram <c>update_id</c> per bot scope for webhook idempotency.
/// Composite primary key: (<see cref="BotScopeKey"/>, <see cref="TelegramUpdateId"/>).
/// </summary>
public sealed class ProcessedTelegramUpdate
{
    public string BotScopeKey { get; private set; } = string.Empty;
    public long TelegramUpdateId { get; private set; }
    public DateTimeOffset ProcessedAtUtc { get; private set; }

    private ProcessedTelegramUpdate()
    {
    }

    public ProcessedTelegramUpdate(string botScopeKey, long telegramUpdateId, DateTimeOffset processedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(botScopeKey))
            throw new ArgumentException("Bot scope key is required.", nameof(botScopeKey));

        BotScopeKey = botScopeKey.Trim();
        TelegramUpdateId = telegramUpdateId;
        ProcessedAtUtc = processedAtUtc;
    }
}
