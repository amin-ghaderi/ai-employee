namespace AiEmployee.Application.Integrations;

/// <summary>
/// Provider-agnostic admin operations for outbound webhook lifecycle. Phase 1 delegates Telegram to <see cref="ITelegramWebhookApplicationService"/>.
/// </summary>
public interface IIntegrationWebhookApplicationService
{
    Task<IntegrationWebhookSyncResult> SyncWebhookAsync(Guid integrationId, CancellationToken cancellationToken = default);

    Task<IntegrationWebhookInfoResult> GetWebhookStatusAsync(Guid integrationId, CancellationToken cancellationToken = default);

    Task<IntegrationWebhookDeleteResult> DeleteWebhookAsync(
        Guid integrationId,
        bool dropPendingUpdates = false,
        CancellationToken cancellationToken = default);
}
