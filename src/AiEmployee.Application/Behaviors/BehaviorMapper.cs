using AiEmployee.Application.Dtos.Behaviors;
using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Application.Behaviors;

public static class BehaviorMapper
{
    public static BehaviorDto ToDto(Behavior behavior)
    {
        return new BehaviorDto
        {
            Id = behavior.Id,
            JudgeContextMessageCount = behavior.JudgeContextMessageCount,
            JudgePerMessageMaxChars = behavior.JudgePerMessageMaxChars,
            JudgeCommandPrefix = behavior.JudgeCommandPrefix,
            ExcludeCommandsFromJudgeContext = behavior.ExcludeCommandsFromJudgeContext,
            OnboardingFirstMessageOnly = behavior.OnboardingFirstMessageOnly,
            LeadFlow = ToLeadFlowDto(behavior.LeadFlow),
            AutomationRules = behavior.AutomationRules.Select(ToAutomationRuleDto).ToList(),
            EngagementRules = ToEngagementRulesDto(behavior.EngagementRules),
            HotLeadPotentialValue = behavior.HotLeadPotentialValue,
            HotLeadTag = behavior.HotLeadTag,
            EnableChat = behavior.EnableChat,
            EnableLead = behavior.EnableLead,
            EnableJudge = behavior.EnableJudge,
            JudgeInstruction = behavior.JudgeInstruction,
            JudgeSchemaJson = behavior.JudgeSchemaJson,
            LeadInstruction = behavior.LeadInstruction,
            LeadSchemaJson = behavior.LeadSchemaJson,
        };
    }

    public static Behavior ToDomain(Guid id, CreateBehaviorRequest request)
    {
        var behavior = new Behavior(
            id,
            request.JudgeContextMessageCount,
            request.JudgePerMessageMaxChars,
            request.JudgeCommandPrefix,
            request.ExcludeCommandsFromJudgeContext,
            request.OnboardingFirstMessageOnly,
            ToLeadFlow(request.LeadFlow),
            (request.AutomationRules ?? Array.Empty<AutomationRuleDto>()).Select(ToAutomationRule).ToList(),
            ToEngagementRules(request.EngagementRules),
            request.HotLeadPotentialValue,
            request.HotLeadTag,
            request.EnableChat,
            request.EnableLead,
            request.EnableJudge);
        behavior.JudgeInstruction = request.JudgeInstruction;
        behavior.JudgeSchemaJson = request.JudgeSchemaJson;
        behavior.LeadInstruction = request.LeadInstruction;
        behavior.LeadSchemaJson = request.LeadSchemaJson;
        return behavior;
    }

    public static Behavior ToDomain(Guid id, UpdateBehaviorRequest request)
    {
        var behavior = new Behavior(
            id,
            request.JudgeContextMessageCount,
            request.JudgePerMessageMaxChars,
            request.JudgeCommandPrefix,
            request.ExcludeCommandsFromJudgeContext,
            request.OnboardingFirstMessageOnly,
            ToLeadFlow(request.LeadFlow),
            (request.AutomationRules ?? Array.Empty<AutomationRuleDto>()).Select(ToAutomationRule).ToList(),
            ToEngagementRules(request.EngagementRules),
            request.HotLeadPotentialValue,
            request.HotLeadTag,
            request.EnableChat,
            request.EnableLead,
            request.EnableJudge);
        behavior.JudgeInstruction = request.JudgeInstruction;
        behavior.JudgeSchemaJson = request.JudgeSchemaJson;
        behavior.LeadInstruction = request.LeadInstruction;
        behavior.LeadSchemaJson = request.LeadSchemaJson;
        return behavior;
    }

    private static LeadFlowDto ToLeadFlowDto(LeadFlow leadFlow) =>
        new()
        {
            FollowUpIndex = leadFlow.FollowUpIndex,
            CaptureIndex = leadFlow.CaptureIndex,
            AnswerKeys = leadFlow.AnswerKeys.ToList(),
        };

    private static EngagementRulesDto ToEngagementRulesDto(EngagementRules rules) =>
        new()
        {
            NewUserWindowHours = rules.NewUserWindowHours,
            ActiveMessageThreshold = rules.ActiveMessageThreshold,
            InactiveHoursThreshold = rules.InactiveHoursThreshold,
            HighEngagementScoreThreshold = rules.HighEngagementScoreThreshold,
            EngagementNormalizationFactor = rules.EngagementNormalizationFactor,
            StickyTags = rules.StickyTags.ToList(),
        };

    private static AutomationRuleDto ToAutomationRuleDto(AutomationRule rule) =>
        new()
        {
            TriggerTag = rule.TriggerTag,
            SuppressIfTagPresent = rule.SuppressIfTagPresent,
            Action = rule.Action,
            MarkTagOnFire = rule.MarkTagOnFire,
        };

    private static LeadFlow ToLeadFlow(LeadFlowDto dto) =>
        new(
            dto.FollowUpIndex,
            dto.CaptureIndex,
            (dto.AnswerKeys ?? Array.Empty<string>()).ToList());

    private static EngagementRules ToEngagementRules(EngagementRulesDto dto) =>
        new(
            dto.NewUserWindowHours,
            dto.ActiveMessageThreshold,
            dto.InactiveHoursThreshold,
            dto.HighEngagementScoreThreshold,
            dto.EngagementNormalizationFactor,
            (dto.StickyTags ?? Array.Empty<string>()).ToList());

    private static AutomationRule ToAutomationRule(AutomationRuleDto dto) =>
        new(
            dto.TriggerTag,
            dto.Action,
            dto.SuppressIfTagPresent,
            dto.MarkTagOnFire);
}
