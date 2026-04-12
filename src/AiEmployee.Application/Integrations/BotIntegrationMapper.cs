using AiEmployee.Application.Dtos.Integrations;
using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Application.Integrations;

public static class BotIntegrationMapper
{
    public static BotIntegrationDto ToDto(BotIntegration integration)
    {
        var providerKey = IntegrationProviders.TryResolveFromChannel(integration.Channel);
        var hasExternal = !string.IsNullOrWhiteSpace(integration.ExternalId);
        var supportsWebhook =
            hasExternal
            && (providerKey is null || IntegrationProviders.SupportsAdminWebhookLifecycle(providerKey));

        return new BotIntegrationDto
        {
            Id = integration.Id,
            BotId = integration.BotId,
            Channel = BotIntegrationChannelNames.NormalizeChannelValue(integration.Channel),
            Provider = providerKey,
            ExternalId = integration.ExternalId,
            IsEnabled = integration.IsEnabled,
            SupportsWebhook = supportsWebhook,
        };
    }
}
