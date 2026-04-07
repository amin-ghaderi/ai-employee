using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Prompting;
using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Application.Services;

public sealed class LeadClassificationService
{
    private readonly IAiClient _aiClient;
    private readonly BehaviorPromptMapper _behaviorPromptMapper;

    public LeadClassificationService(IAiClient aiClient, BehaviorPromptMapper behaviorPromptMapper)
    {
        _aiClient = aiClient;
        _behaviorPromptMapper = behaviorPromptMapper;
    }

    public async Task<(string userType, string intent, string potential)> ClassifyAsync(
        Persona persona,
        Behavior behavior,
        Dictionary<string, string> answers,
        IReadOnlyList<string> answerKeys)
    {
        var executionContext = BuildExecutionContext(persona, behavior, answers, answerKeys);

        var result = await _aiClient.ClassifyLeadAsync(executionContext.Prompt);

        return (result.UserType, result.Intent, result.Potential);
    }

    public LeadExecutionContext BuildExecutionContext(
        Persona persona,
        Behavior behavior,
        Dictionary<string, string> answers,
        IReadOnlyList<string> answerKeys)
    {
        var goal = answerKeys.Count > 0 && answers.ContainsKey(answerKeys[0])
            ? answers[answerKeys[0]]
            : "";
        var experience = answerKeys.Count > 1 && answers.ContainsKey(answerKeys[1])
            ? answers[answerKeys[1]]
            : "";

        var leadTemplate = _behaviorPromptMapper.BuildLeadPrompt(persona, behavior);

        var prompt = leadTemplate
            .Replace(PromptTokens.Goal, goal, StringComparison.Ordinal)
            .Replace(PromptTokens.Experience, experience, StringComparison.Ordinal);

        return new LeadExecutionContext
        {
            Prompt = prompt,
        };
    }
}
