using AiEmployee.Application.BotConfig;

namespace AiEmployee.Application.Interfaces;

public interface IBotConfigurationRepository
{
    /// <summary>Includes bot graph and the <c>JudgeTranscriptWrapper</c> prompt template from storage.</summary>
    Task<JudgeBotConfiguration> GetJudgeBotAsync();

    /// <summary>Resolves configuration for an external integration; falls back to <see cref="GetJudgeBotAsync"/> when no match.</summary>
    Task<JudgeBotConfiguration> GetByIntegrationAsync(string channel, string externalId);
}
