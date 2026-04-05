namespace AiEmployee.Domain.BotConfiguration;

public sealed class PromptSections
{
    public string System { get; private set; } = string.Empty;
    public string Judge { get; private set; } = string.Empty;
    public string Lead { get; private set; } = string.Empty;

    private PromptSections()
    {
    }

    public PromptSections(string system, string judge, string lead)
    {
        System = system;
        Judge = judge;
        Lead = lead;
    }
}
