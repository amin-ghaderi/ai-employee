using AiEmployee.Application.Integrations;
using AiEmployee.Application.Integrations.Providers;
using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Infrastructure.Integrations.Telegram;

/// <summary>Telegram Bot API integration: webhook lifecycle delegates to <see cref="ITelegramWebhookApplicationService"/>.</summary>
public sealed class TelegramIntegrationProvider : IIntegrationProvider
{
    private readonly ITelegramWebhookApplicationService _telegramWebhook;

    public TelegramIntegrationProvider(ITelegramWebhookApplicationService telegramWebhook)
    {
        _telegramWebhook = telegramWebhook;
    }

    /// <inheritdoc />
    public string ProviderId => IntegrationProviders.Telegram;

    /// <inheritdoc />
    public bool SupportsWebhookLifecycle => true;

    /// <inheritdoc />
    public async Task<IntegrationWebhookSyncResult> SyncWebhookAsync(
        BotIntegration integration,
        CancellationToken cancellationToken)
    {
        var result = await _telegramWebhook.SyncWebhookAsync(integration.Id, cancellationToken).ConfigureAwait(false);
        return IntegrationWebhookMapper.FromTelegram(result);
    }

    /// <inheritdoc />
    public async Task<IntegrationWebhookInfoResult> GetWebhookInfoAsync(
        BotIntegration integration,
        CancellationToken cancellationToken)
    {
        var result = await _telegramWebhook.GetWebhookStatusAsync(integration.Id, cancellationToken).ConfigureAwait(false);
        return IntegrationWebhookMapper.FromTelegram(result);
    }

    /// <inheritdoc />
    public async Task<IntegrationWebhookDeleteResult> DeleteWebhookAsync(
        BotIntegration integration,
        bool dropPendingUpdates,
        CancellationToken cancellationToken)
    {
        var result = await _telegramWebhook.DeleteWebhookAsync(integration.Id, dropPendingUpdates, cancellationToken)
            .ConfigureAwait(false);
        return IntegrationWebhookMapper.FromTelegram(result);
    }
}
