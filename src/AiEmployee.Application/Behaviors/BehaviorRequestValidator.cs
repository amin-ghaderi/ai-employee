using AiEmployee.Application.Dtos.Behaviors;
using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Application.Behaviors;

public static class BehaviorRequestValidator
{
    public static void Validate(CreateBehaviorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = CollectErrors(
            request.JudgeContextMessageCount,
            request.JudgePerMessageMaxChars,
            request.LeadFlow,
            request.EngagementRules,
            request.AutomationRules);
        AppendGatewayMatchTypeErrors(errors, request.GatewayMatchType);
        if (errors.Count > 0)
            throw new BehaviorValidationException(errors);
    }

    public static void Validate(UpdateBehaviorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = CollectErrors(
            request.JudgeContextMessageCount,
            request.JudgePerMessageMaxChars,
            request.LeadFlow,
            request.EngagementRules,
            request.AutomationRules);
        AppendGatewayMatchTypeErrors(errors, request.GatewayMatchType);
        if (errors.Count > 0)
            throw new BehaviorValidationException(errors);
    }

    private static void AppendGatewayMatchTypeErrors(List<string> errors, int gatewayMatchType)
    {
        if (!Enum.IsDefined(typeof(GatewayPhraseMatchType), gatewayMatchType))
            errors.Add("gatewayMatchType must be 0 (Contains), 1 (Exact), or 2 (Regex).");
    }

    private static List<string> CollectErrors(
        int judgeContextMessageCount,
        int judgePerMessageMaxChars,
        LeadFlowDto? leadFlow,
        EngagementRulesDto? engagementRules,
        IReadOnlyList<AutomationRuleDto>? automationRules)
    {
        var errors = new List<string>();

        if (judgeContextMessageCount <= 0)
            errors.Add("judgeContextMessageCount must be greater than 0.");

        if (judgePerMessageMaxChars <= 0)
            errors.Add("judgePerMessageMaxChars must be greater than 0.");

        if (leadFlow is null)
            errors.Add("leadFlow is required.");
        else
        {
            if (leadFlow.AnswerKeys is null)
                errors.Add("leadFlow.answerKeys must not be null.");
            else
            {
                for (var i = 0; i < leadFlow.AnswerKeys.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(leadFlow.AnswerKeys[i]))
                        errors.Add($"leadFlow.answerKeys[{i}] must not be null or empty.");
                }
            }

            var answerCount = leadFlow.AnswerKeys?.Count ?? 0;

            if (leadFlow.CaptureIndex is not null && answerCount == 0)
                errors.Add("When captureIndex is set, answerKeys must contain at least one entry.");

            if (leadFlow.FollowUpIndex is not null && leadFlow.FollowUpIndex.Value < 0)
                errors.Add("When followUpIndex is set, it must be >= 0.");

            if (leadFlow.FollowUpIndex is not null
                && leadFlow.CaptureIndex is not null
                && leadFlow.CaptureIndex.Value < leadFlow.FollowUpIndex.Value)
                errors.Add("When both followUpIndex and captureIndex are set, captureIndex must be >= followUpIndex.");
        }

        if (engagementRules is null)
            errors.Add("engagementRules is required.");
        else
        {
            if (engagementRules.EngagementNormalizationFactor <= 0)
                errors.Add("engagementNormalizationFactor must be greater than 0.");

            if (engagementRules.ActiveMessageThreshold < 0)
                errors.Add("activeMessageThreshold must be >= 0.");

            if (engagementRules.InactiveHoursThreshold < 0)
                errors.Add("inactiveHoursThreshold must be >= 0.");

            if (engagementRules.NewUserWindowHours < 0)
                errors.Add("newUserWindowHours must be >= 0.");
        }

        var rules = automationRules ?? Array.Empty<AutomationRuleDto>();
        for (var i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            if (rule is null)
            {
                errors.Add($"automationRules[{i}] must not be null.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(rule.TriggerTag))
                errors.Add($"automationRules[{i}].triggerTag must not be null or empty.");

            if (!Enum.IsDefined(typeof(AutomationActionKind), rule.Action))
                errors.Add($"automationRules[{i}].action must be a valid AutomationActionKind value.");
        }

        return errors;
    }
}
