using System.Diagnostics;
using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Prompting;
using AiEmployee.Application.Services;
using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Application.Admin;

public sealed class LeadExecutionService : ILeadExecutionService
{
    private readonly LeadClassificationService _leadService;
    private readonly IAiClient _aiClient;

    public LeadExecutionService(
        LeadClassificationService leadService,
        IAiClient aiClient)
    {
        _leadService = leadService;
        _aiClient = aiClient;
    }

    public async Task<LeadExecutionResult> ExecuteWithDebugAsync(
        Persona persona,
        Behavior behavior,
        List<string> answers,
        List<string> answerKeys,
        CancellationToken cancellationToken = default)
    {
        AiDebugContext.Clear();

        var safeAnswers = answers ?? [];
        var safeAnswerKeys = answerKeys ?? [];
        var answersMap = new Dictionary<string, string>(StringComparer.Ordinal);
        var count = Math.Min(safeAnswers.Count, safeAnswerKeys.Count);
        for (var i = 0; i < count; i++)
        {
            var key = safeAnswerKeys[i];
            if (!string.IsNullOrWhiteSpace(key))
                answersMap[key] = safeAnswers[i] ?? string.Empty;
        }

        var ctx = _leadService.BuildExecutionContext(
            persona,
            behavior,
            answersMap,
            safeAnswerKeys);

        var source = BehaviorPromptMapper.GetLeadPromptSource(behavior);

        var schema = BehaviorPromptMapper.ParseSchema(behavior.LeadSchemaJson);

        var sw = Stopwatch.StartNew();
        var result = await _aiClient.ClassifyLeadAsync(ctx.Prompt);
        sw.Stop();

        var raw = AiDebugContext.GetLastRawResponse();

        return new LeadExecutionResult
        {
            UserType = result.UserType,
            Intent = result.Intent,
            Potential = result.Potential,
            Debug = new LeadDebugResponse
            {
                Prompt = ctx.Prompt,
                PromptSource = source,
                Schema = schema,
                ParsedResult = result,
                RawResponse = raw,
                LatencyMs = sw.ElapsedMilliseconds,
                PersonaId = persona.Id,
                BehaviorId = behavior.Id,
            },
        };
    }
}
