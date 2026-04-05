using AiEmployee.Application.BotConfig;

namespace AiEmployee.Application.Admin;

public interface IAdminConfigService
{
    Task<JudgeBotConfiguration> GetConfigAsync(Guid botId, CancellationToken cancellationToken = default);

    Task UpdateConfigAsync(Guid botId, UpdateBotConfigRequest request, CancellationToken cancellationToken = default);
}
