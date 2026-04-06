using AiEmployee.Application.Dtos.Integrations;
using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Application.Integrations;

public static class BotIntegrationMapper
{
    public static BotIntegrationDto ToDto(BotIntegration integration)
    {
        return new BotIntegrationDto
        {
            Id = integration.Id,
            BotId = integration.BotId,
            Channel = integration.Channel,
            ExternalId = integration.ExternalId,
            IsEnabled = integration.IsEnabled,
        };
    }
}
