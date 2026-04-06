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
    public EngagementRules EngagementRules { get; private set; } = null!;
    public string HotLeadPotentialValue { get; private set; } = string.Empty;
    public string HotLeadTag { get; private set; } = string.Empty;
    public bool EnableChat { get; private set; }
    public bool EnableLead { get; private set; }
    public bool EnableJudge { get; private set; }

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
        IReadOnlyList<AutomationRule> automationRules,
        EngagementRules engagementRules,
        string hotLeadPotentialValue,
        string hotLeadTag,
        bool enableChat = true,
        bool enableLead = true,
        bool enableJudge = true)
    {
        Id = id;
        JudgeContextMessageCount = judgeContextMessageCount;
        JudgePerMessageMaxChars = judgePerMessageMaxChars;
        JudgeCommandPrefix = judgeCommandPrefix;
        ExcludeCommandsFromJudgeContext = excludeCommandsFromJudgeContext;
        OnboardingFirstMessageOnly = onboardingFirstMessageOnly;
        LeadFlow = leadFlow;
        AutomationRules = automationRules ?? Array.Empty<AutomationRule>();
        EngagementRules = engagementRules;
        HotLeadPotentialValue = hotLeadPotentialValue;
        HotLeadTag = hotLeadTag;
        EnableChat = enableChat;
        EnableLead = enableLead;
        EnableJudge = enableJudge;
    }

    public void ReplaceConfiguration(
        int judgeContextMessageCount,
        int judgePerMessageMaxChars,
        string judgeCommandPrefix,
        bool excludeCommandsFromJudgeContext,
        bool onboardingFirstMessageOnly,
        LeadFlow leadFlow,
        IReadOnlyList<AutomationRule> automationRules,
        EngagementRules engagementRules,
        string hotLeadPotentialValue,
        string hotLeadTag,
        bool enableChat,
        bool enableLead,
        bool enableJudge)
    {
        JudgeContextMessageCount = judgeContextMessageCount;
        JudgePerMessageMaxChars = judgePerMessageMaxChars;
        JudgeCommandPrefix = judgeCommandPrefix;
        ExcludeCommandsFromJudgeContext = excludeCommandsFromJudgeContext;
        OnboardingFirstMessageOnly = onboardingFirstMessageOnly;
        LeadFlow = leadFlow;
        AutomationRules = automationRules ?? Array.Empty<AutomationRule>();
        EngagementRules = engagementRules;
        HotLeadPotentialValue = hotLeadPotentialValue;
        HotLeadTag = hotLeadTag;
        EnableChat = enableChat;
        EnableLead = enableLead;
        EnableJudge = enableJudge;
    }
}
