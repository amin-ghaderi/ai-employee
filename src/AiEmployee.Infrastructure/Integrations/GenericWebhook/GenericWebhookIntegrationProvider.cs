using AiEmployee.Application.Integrations;
using AiEmployee.Application.Integrations.Providers;
using AiEmployee.Domain.BotConfiguration;
using Microsoft.Extensions.Hosting;

namespace AiEmployee.Infrastructure.Integrations.GenericWebhook;

/// <summary>
/// Admin webhook lifecycle for arbitrary HTTPS callback URLs. <see cref="BotIntegration.ExternalId"/> stores the full webhook URL;
/// sync validates the URL; there is no remote vendor API to register against.
/// </summary>
public sealed class GenericWebhookIntegrationProvider : IIntegrationProvider
{
    private readonly IHostEnvironment _environment;

    public GenericWebhookIntegrationProvider(IHostEnvironment environment)
    {
        _environment = environment;
    }

    /// <inheritdoc />
    public string ProviderId => IntegrationProviders.GenericWebhook;

    /// <inheritdoc />
    public bool SupportsWebhookLifecycle => true;

    /// <inheritdoc />
    public Task<IntegrationWebhookSyncResult> SyncWebhookAsync(
        BotIntegration integration,
        CancellationToken cancellationToken)
    {
        var validation = ValidateWebhookUrl(integration.ExternalId);
        if (validation is not null)
            return Task.FromResult(validation);

        var url = integration.ExternalId.Trim();
        return Task.FromResult(IntegrationWebhookSyncResult.Ok(url, "Generic webhook URL validated locally."));
    }

    /// <inheritdoc />
    public Task<IntegrationWebhookInfoResult> GetWebhookInfoAsync(
        BotIntegration integration,
        CancellationToken cancellationToken)
    {
        var validation = ValidateWebhookUrl(integration.ExternalId);
        if (validation is not null)
        {
            return Task.FromResult(
                new IntegrationWebhookInfoResult(
                    false,
                    validation.FailureCategory,
                    validation.Message,
                    validation.ProviderErrorCode,
                    validation.ProviderDescription,
                    null));
        }

        var url = integration.ExternalId.Trim();
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
        _ = dropPendingUpdates;
        return Task.FromResult(
            new IntegrationWebhookDeleteResult(true, IntegrationWebhookFailureCategory.None, null, null, null));
    }

    private IntegrationWebhookSyncResult? ValidateWebhookUrl(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return IntegrationWebhookSyncResult.Failed(
                IntegrationWebhookFailureCategory.BadRequestGuard,
                "Webhook URL is missing.");
        }

        if (!Uri.TryCreate(rawUrl.Trim(), UriKind.Absolute, out var uri))
        {
            return IntegrationWebhookSyncResult.Failed(
                IntegrationWebhookFailureCategory.BadRequestGuard,
                "Webhook URL must be an absolute URI.");
        }

        if (IsProduction() && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return IntegrationWebhookSyncResult.Failed(
                IntegrationWebhookFailureCategory.BadRequestGuard,
                "Webhook URL must use HTTPS in production.");
        }

        return null;
    }

    private bool IsProduction() =>
        string.Equals(_environment.EnvironmentName, Environments.Production, StringComparison.OrdinalIgnoreCase);
}
