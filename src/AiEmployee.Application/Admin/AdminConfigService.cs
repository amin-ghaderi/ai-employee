using AiEmployee.Application.BotConfig;
using AiEmployee.Application.Interfaces;

namespace AiEmployee.Application.Admin;

public sealed class AdminConfigService : IAdminConfigService
{
    private readonly IBotConfigurationRepository _repository;
    private readonly IBotConfigurationCommand _botConfigurationCommand;

    public AdminConfigService(
        IBotConfigurationRepository repository,
        IBotConfigurationCommand botConfigurationCommand)
    {
        _repository = repository;
        _botConfigurationCommand = botConfigurationCommand;
    }

    public async Task<JudgeBotConfiguration> GetConfigAsync(Guid botId, CancellationToken cancellationToken = default)
    {
        var config = await _repository.GetJudgeBotAsync().ConfigureAwait(false);

        if (config.Bot.Id != botId)
            throw new KeyNotFoundException($"No bot configuration was found for id '{botId}'.");

        return config;
    }

    public async Task UpdateConfigAsync(Guid botId, UpdateBotConfigRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.JudgePrompt))
            throw new ArgumentException("JudgePrompt cannot be null or empty.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.LeadPrompt))
            throw new ArgumentException("LeadPrompt cannot be null or empty.", nameof(request));

        await _botConfigurationCommand
            .UpdatePromptsAsync(botId, request.JudgePrompt, request.LeadPrompt, cancellationToken)
            .ConfigureAwait(false);
    }
}
