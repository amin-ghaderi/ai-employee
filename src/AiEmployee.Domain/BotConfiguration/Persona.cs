namespace AiEmployee.Domain.BotConfiguration;

public sealed class Persona
{
    public Guid Id { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public PromptSections Prompts { get; private set; } = null!;
    public ClassificationSchema ClassificationSchema { get; private set; } = null!;

    private Persona()
    {
    }

    public Persona(
        Guid id,
        string displayName,
        PromptSections prompts,
        ClassificationSchema classificationSchema)
    {
        Id = id;
        DisplayName = displayName;
        Prompts = prompts;
        ClassificationSchema = classificationSchema;
    }

    public void UpdateJudgeAndLeadPrompts(string judgePrompt, string leadPrompt)
    {
        if (string.IsNullOrWhiteSpace(judgePrompt))
            throw new ArgumentException("Judge prompt cannot be null or whitespace.", nameof(judgePrompt));

        if (string.IsNullOrWhiteSpace(leadPrompt))
            throw new ArgumentException("Lead prompt cannot be null or whitespace.", nameof(leadPrompt));

        Prompts = new PromptSections(Prompts.System, judgePrompt, leadPrompt);
    }
}
