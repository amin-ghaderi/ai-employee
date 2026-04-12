using AiEmployee.Application.Integrations;
using AiEmployee.Application.Integrations.Providers;
using AiEmployee.Domain.BotConfiguration;
using Microsoft.Extensions.Logging;

namespace AiEmployee.Infrastructure.Integrations.Slack;

/// <summary>
/// Slack Events API: the Request URL is configured in the Slack app; admin &quot;sync&quot; only validates the stored URL in <see cref="BotIntegration.ExternalId"/>.
/// </summary>
public sealed class SlackIntegrationProvider : IIntegrationProvider
{
    private readonly ILogger<SlackIntegrationProvider> _logger;

    public SlackIntegrationProvider(ILogger<SlackIntegrationProvider> logger) => _logger = logger;

    /// <inheritdoc />
    public string ProviderId => IntegrationProviders.Slack;

    /// <inheritdoc />
    public bool SupportsWebhookLifecycle => true;

    /// <inheritdoc />
    public Task<IntegrationWebhookSyncResult> SyncWebhookAsync(
        BotIntegration integration,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var url = integration.ExternalId?.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            return Task.FromResult(
                IntegrationWebhookSyncResult.Failed(
                    IntegrationWebhookFailureCategory.BadRequestGuard,
                    "Slack Request URL is missing."));
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            return Task.FromResult(
                IntegrationWebhookSyncResult.Failed(
                    IntegrationWebhookFailureCategory.BadRequestGuard,
                    "Slack Request URL must be an absolute HTTPS URI."));
        }

        _logger.LogInformation(
            "slack_webhook_sync_validated | integrationId={IntegrationId} urlLength={Len}",
            integration.Id,
            url.Length);

        return Task.FromResult(
            IntegrationWebhookSyncResult.Ok(url, "Slack Events Request URL validated locally."));
    }

    /// <inheritdoc />
    public Task<IntegrationWebhookInfoResult> GetWebhookInfoAsync(
        BotIntegration integration,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var url = integration.ExternalId?.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            return Task.FromResult(
                new IntegrationWebhookInfoResult(
                    false,
                    IntegrationWebhookFailureCategory.BadRequestGuard,
                    "Slack Request URL is missing.",
                    null,
                    null,
                    null));
        }

        var info = new IntegrationWebhookInfoData(url, 0, null, null, false, null);
        return Task.FromResult(
            new IntegrationWebhookInfoResult(true, IntegrationWebhookFailureCategory.None, null, null, null, info));
    }

    /// <inheritdoc />
    public Task<IntegrationWebhookDeleteResult> DeleteWebhookAsync(
        BotIntegration integration,
        bool dropPendingUpdates,
        CancellationToken cancellationToken)
    {
        _ = integration;
        _ = dropPendingUpdates;
        _ = cancellationToken;
        return Task.FromResult(
            new IntegrationWebhookDeleteResult(true, IntegrationWebhookFailureCategory.None, null, null, null));
    }
}
