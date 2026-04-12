namespace AiEmployee.Application.Integrations;

/// <summary>Orchestrates Telegram webhook URLs using <see cref="Options.IPublicBaseUrlProvider"/> and integration tokens.</summary>
public interface ITelegramWebhookApplicationService
{
    /// <summary>Registers Telegram webhook to <c>{PublicBaseUrl}/api/telegram/webhook/{integrationId}</c>.</summary>
    Task<TelegramWebhookSyncResult> SyncWebhookAsync(Guid integrationId, CancellationToken cancellationToken = default);

    /// <summary>Reads current webhook info from Telegram.</summary>
    Task<TelegramWebhookInfoResult> GetWebhookStatusAsync(Guid integrationId, CancellationToken cancellationToken = default);

    /// <summary>Removes webhook for the bot.</summary>
    Task<TelegramWebhookDeleteResult> DeleteWebhookAsync(
        Guid integrationId,
        bool dropPendingUpdates = false,
        CancellationToken cancellationToken = default);
}
