using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Application.Integrations.Providers;

/// <summary>Strategy for outbound webhook lifecycle for a single integration channel (Telegram, WhatsApp, …).</summary>
public interface IIntegrationProvider
{
    string ProviderId { get; }

    bool SupportsWebhookLifecycle { get; }

    Task<IntegrationWebhookSyncResult> SyncWebhookAsync(
        BotIntegration integration,
        CancellationToken cancellationToken);

    Task<IntegrationWebhookInfoResult> GetWebhookInfoAsync(
        BotIntegration integration,
        CancellationToken cancellationToken);

    Task<IntegrationWebhookDeleteResult> DeleteWebhookAsync(
        BotIntegration integration,
        bool dropPendingUpdates,
        CancellationToken cancellationToken);
}
