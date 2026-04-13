namespace AiEmployee.Application.Interfaces;

/// <summary>
/// Telegram webhook idempotency: first successful insert for (bot scope, <c>update_id</c>) wins.
/// </summary>
public interface ITelegramUpdateDeduplicator
{
    /// <summary>
    /// Attempts to record that this Telegram update is being processed.
    /// </summary>
    /// <returns><see langword="true"/> if this is the first delivery; <see langword="false"/> if duplicate.</returns>
    Task<bool> TryRegisterFirstDeliveryAsync(string botScopeKey, long telegramUpdateId, CancellationToken cancellationToken = default);
}
