using AiEmployee.Application.Interfaces;
using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Application.Services;

public sealed class LeadClassificationService
{
    private const string GoalPlaceholder = "{{goal}}";
    private const string ExperiencePlaceholder = "{{experience}}";

    private readonly IAiClient _aiClient;

    public LeadClassificationService(IAiClient aiClient)
    {
        _aiClient = aiClient;
    }

    public async Task<(string userType, string intent, string potential)> ClassifyAsync(
        Persona persona,
        Dictionary<string, string> answers,
        IReadOnlyList<string> answerKeys)
    {
        var prompt = BuildPrompt(persona, answers, answerKeys);

        var result = await _aiClient.ClassifyLeadAsync(prompt);

        return (result.UserType, result.Intent, result.Potential);
    }

    private static string BuildPrompt(
        Persona persona,
        Dictionary<string, string> answers,
        IReadOnlyList<string> answerKeys)
    {
        var goal = answerKeys.Count > 0 && answers.ContainsKey(answerKeys[0])
            ? answers[answerKeys[0]]
            : "";
        var experience = answerKeys.Count > 1 && answers.ContainsKey(answerKeys[1])
            ? answers[answerKeys[1]]
            : "";

        return persona.Prompts.Lead
            .Replace(GoalPlaceholder, goal, StringComparison.Ordinal)
            .Replace(ExperiencePlaceholder, experience, StringComparison.Ordinal);
    }
}
