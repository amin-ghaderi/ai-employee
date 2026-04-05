namespace AiEmployee.Domain.BotConfiguration;

public sealed class LanguageProfile
{
    public Guid Id { get; private set; }
    public string Locale { get; private set; } = string.Empty;
    public FormalityLevel Formality { get; private set; }
    public string OnboardingGoalQuestion { get; private set; } = string.Empty;
    public string ExperienceFollowUpQuestion { get; private set; } = string.Empty;
    public string LeadThanksMessage { get; private set; } = string.Empty;
    public string JudgeNoConversationMessage { get; private set; } = string.Empty;
    public string JudgeNotEnoughContextMessage { get; private set; } = string.Empty;
    public string JudgeResultTemplate { get; private set; } = string.Empty;
    public string GenericErrorMessage { get; private set; } = string.Empty;
    public string ReactivationMessage { get; private set; } = string.Empty;

    private LanguageProfile()
    {
    }

    public LanguageProfile(
        Guid id,
        string locale,
        FormalityLevel formality,
        string onboardingGoalQuestion,
        string experienceFollowUpQuestion,
        string leadThanksMessage,
        string judgeNoConversationMessage,
        string judgeNotEnoughContextMessage,
        string judgeResultTemplate,
        string genericErrorMessage,
        string reactivationMessage)
    {
        Id = id;
        Locale = locale;
        Formality = formality;
        OnboardingGoalQuestion = onboardingGoalQuestion;
        ExperienceFollowUpQuestion = experienceFollowUpQuestion;
        LeadThanksMessage = leadThanksMessage;
        JudgeNoConversationMessage = judgeNoConversationMessage;
        JudgeNotEnoughContextMessage = judgeNotEnoughContextMessage;
        JudgeResultTemplate = judgeResultTemplate;
        GenericErrorMessage = genericErrorMessage;
        ReactivationMessage = reactivationMessage;
    }
}
