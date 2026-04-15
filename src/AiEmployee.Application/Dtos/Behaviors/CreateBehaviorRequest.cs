namespace AiEmployee.Application.Dtos.Behaviors;

public sealed class CreateBehaviorRequest
{
    public int JudgeContextMessageCount { get; set; }
    public int JudgePerMessageMaxChars { get; set; }
    public string JudgeCommandPrefix { get; set; } = string.Empty;
    public bool ExcludeCommandsFromJudgeContext { get; set; }
    public bool OnboardingFirstMessageOnly { get; set; }
    public LeadFlowDto LeadFlow { get; set; } = null!;
    public IReadOnlyList<AutomationRuleDto> AutomationRules { get; set; } = Array.Empty<AutomationRuleDto>();
    public EngagementRulesDto EngagementRules { get; set; } = null!;
    public string HotLeadPotentialValue { get; set; } = string.Empty;
    public string HotLeadTag { get; set; } = string.Empty;
    public bool EnableChat { get; set; } = true;
    public bool EnableLead { get; set; } = true;
    public bool EnableJudge { get; set; } = true;

    public bool EnableGatewayRouting { get; set; }
    public string? GatewayTriggerPhrases { get; set; }
    public int GatewayMatchType { get; set; }
    public bool GatewayCaseSensitive { get; set; }
}
