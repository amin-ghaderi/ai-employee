namespace AiEmployee.Domain.BotConfiguration;

public sealed class Behavior
{
    public Guid Id { get; private set; }
    public int JudgeContextMessageCount { get; private set; }
    public int JudgePerMessageMaxChars { get; private set; }
    public string JudgeCommandPrefix { get; private set; } = string.Empty;
    public bool ExcludeCommandsFromJudgeContext { get; private set; }
    public bool OnboardingFirstMessageOnly { get; private set; }
    public LeadFlow LeadFlow { get; private set; } = null!;
    public IReadOnlyList<AutomationRule> AutomationRules { get; private set; } = Array.Empty<AutomationRule>();

    private Behavior()
    {
    }

    public Behavior(
        Guid id,
        int judgeContextMessageCount,
        int judgePerMessageMaxChars,
        string judgeCommandPrefix,
        bool excludeCommandsFromJudgeContext,
        bool onboardingFirstMessageOnly,
        LeadFlow leadFlow,
        IReadOnlyList<AutomationRule> automationRules)
    {
        Id = id;
        JudgeContextMessageCount = judgeContextMessageCount;
        JudgePerMessageMaxChars = judgePerMessageMaxChars;
        JudgeCommandPrefix = judgeCommandPrefix;
        ExcludeCommandsFromJudgeContext = excludeCommandsFromJudgeContext;
        OnboardingFirstMessageOnly = onboardingFirstMessageOnly;
        LeadFlow = leadFlow;
        AutomationRules = automationRules ?? Array.Empty<AutomationRule>();
    }
}
