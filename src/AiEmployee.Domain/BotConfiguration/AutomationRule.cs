namespace AiEmployee.Domain.BotConfiguration;

public sealed class AutomationRule
{
    public string TriggerTag { get; private set; } = string.Empty;
    public string? SuppressIfTagPresent { get; private set; }
    public AutomationActionKind Action { get; private set; }
    public string? MarkTagOnFire { get; private set; }

    private AutomationRule()
    {
    }

    public AutomationRule(
        string triggerTag,
        AutomationActionKind action,
        string? suppressIfTagPresent = null,
        string? markTagOnFire = null)
    {
        TriggerTag = triggerTag;
        Action = action;
        SuppressIfTagPresent = suppressIfTagPresent;
        MarkTagOnFire = markTagOnFire;
    }
}
