using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Integrations.Providers;
using AiEmployee.Domain.BotConfiguration;
using Microsoft.Extensions.Logging;

namespace AiEmployee.Application.Integrations;

/// <summary>
/// Facade over <see cref="IIntegrationProvider"/> implementations. Resolves provider via <see cref="IIntegrationProviderRegistry"/>.
/// </summary>
public sealed class IntegrationWebhookApplicationService : IIntegrationWebhookApplicationService
{
    private readonly IBotIntegrationRepository _integrations;
    private readonly IIntegrationProviderRegistry _registry;
    private readonly ILogger<IntegrationWebhookApplicationService> _logger;

    public IntegrationWebhookApplicationService(
        IBotIntegrationRepository integrations,
        IIntegrationProviderRegistry registry,
        ILogger<IntegrationWebhookApplicationService> logger)
    {
        _integrations = integrations;
        _registry = registry;
        _logger = logger;
    }

    public async Task<IntegrationWebhookSyncResult> SyncWebhookAsync(
        Guid integrationId,
        CancellationToken cancellationToken = default)
    {
        var integration = await _integrations.GetByIdAsync(integrationId, cancellationToken).ConfigureAwait(false);
        if (integration is null)
        {
            return IntegrationWebhookSyncResult.Failed(
                IntegrationWebhookFailureCategory.IntegrationNotFound,
                $"No integration found for id '{integrationId}'.");
        }

        if (!TryResolveWebhookProvider(integration, out var provider, out var guardFailure))
            return guardFailure!;

        return await provider!.SyncWebhookAsync(integration, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IntegrationWebhookInfoResult> GetWebhookStatusAsync(
        Guid integrationId,
        CancellationToken cancellationToken = default)
    {
        var integration = await _integrations.GetByIdAsync(integrationId, cancellationToken).ConfigureAwait(false);
        if (integration is null)
        {
            return new IntegrationWebhookInfoResult(
                false,
                IntegrationWebhookFailureCategory.IntegrationNotFound,
                $"No integration found for id '{integrationId}'.",
                null,
                null,
                null);
        }

        if (!TryResolveWebhookProvider(integration, out var provider, out var guardFailure))
            return MapSyncGuardToInfoResult(guardFailure!);

        return await provider!.GetWebhookInfoAsync(integration, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IntegrationWebhookDeleteResult> DeleteWebhookAsync(
        Guid integrationId,
        bool dropPendingUpdates = false,
        CancellationToken cancellationToken = default)
    {
        var integration = await _integrations.GetByIdAsync(integrationId, cancellationToken).ConfigureAwait(false);
        if (integration is null)
        {
            return new IntegrationWebhookDeleteResult(
                false,
                IntegrationWebhookFailureCategory.IntegrationNotFound,
                $"No integration found for id '{integrationId}'.",
                null,
                null);
        }

        if (!TryResolveWebhookProvider(integration, out var provider, out var guardFailure))
            return MapSyncGuardToDeleteResult(guardFailure!);

        return await provider!.DeleteWebhookAsync(integration, dropPendingUpdates, cancellationToken)
            .ConfigureAwait(false);
    }

    private bool TryResolveWebhookProvider(
        BotIntegration integration,
        out IIntegrationProvider? provider,
        out IntegrationWebhookSyncResult? guardFailure)
    {
        guardFailure = null;
        provider = null;

        var providerKey = IntegrationProviders.TryResolveFromChannel(integration.Channel);
        var resolved = _registry.Resolve(providerKey);
        if (resolved is null)
        {
            _logger.LogDebug(
                "webhook_guard_no_provider | integrationId={IntegrationId} providerKey={ProviderKey}",
                integration.Id,
                providerKey ?? "(null)");

            guardFailure = IntegrationWebhookSyncResult.Failed(
                IntegrationWebhookFailureCategory.BadRequestGuard,
                "Integration is not a Telegram integration.");
            return false;
        }

        if (!resolved.SupportsWebhookLifecycle)
        {
            guardFailure = IntegrationWebhookSyncResult.Failed(
                IntegrationWebhookFailureCategory.UnsupportedProvider,
                "Integration does not support webhook lifecycle operations.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(integration.ExternalId))
        {
            var missingCredentialMessage =
                string.Equals(providerKey, IntegrationProviders.GenericWebhook, StringComparison.Ordinal)
                    ? "Webhook URL is missing."
                    : string.Equals(providerKey, IntegrationProviders.WhatsApp, StringComparison.Ordinal)
                        ? "Webhook callback URL is missing."
                        : string.Equals(providerKey, IntegrationProviders.Slack, StringComparison.Ordinal)
                            ? "Slack Request URL is missing."
                            : "Telegram bot token (ExternalId) is missing.";
            guardFailure = IntegrationWebhookSyncResult.Failed(
                IntegrationWebhookFailureCategory.BadRequestGuard,
                missingCredentialMessage);
            return false;
        }

        provider = resolved;
        return true;
    }

    private static IntegrationWebhookInfoResult MapSyncGuardToInfoResult(IntegrationWebhookSyncResult s) =>
        new(
            false,
            s.FailureCategory,
            s.Message,
            s.ProviderErrorCode,
            s.ProviderDescription,
            null);

    private static IntegrationWebhookDeleteResult MapSyncGuardToDeleteResult(IntegrationWebhookSyncResult s) =>
        new(false, s.FailureCategory, s.Message, s.ProviderErrorCode, s.ProviderDescription);
}
