namespace AiEmployee.Domain.BotConfiguration;

/// <summary>
/// Aggregate for assistant identity, classification lists, and all prompt text/schema used by the AI pipeline.
/// <see cref="Prompts.System"/> is the general chat instruction (admin UI label: Chat Instruction).
/// Judge/lead instruction and schema fields pair with <see cref="PromptSections"/> judge/lead templates for structured output.
/// </summary>
public sealed class Persona
{
    public Guid Id { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public PromptSections Prompts { get; private set; } = null!;
    public ClassificationSchema ClassificationSchema { get; private set; } = null!;

    /// <summary>Optional JSON schema for general chat completions; empty means unconstrained output.</summary>
    public string? ChatOutputSchemaJson { get; private set; }

    /// <summary>Optional judge instruction override (paired with <see cref="JudgeSchemaJson"/>).</summary>
    public string? JudgeInstruction { get; private set; }

    /// <summary>Optional JSON schema for judge output.</summary>
    public string? JudgeSchemaJson { get; private set; }

    /// <summary>Optional lead classifier instruction (paired with <see cref="LeadSchemaJson"/>).</summary>
    public string? LeadInstruction { get; private set; }

    /// <summary>Optional JSON schema for lead classification output.</summary>
    public string? LeadSchemaJson { get; private set; }

    private Persona()
    {
    }

    public Persona(
        Guid id,
        string displayName,
        PromptSections prompts,
        ClassificationSchema classificationSchema,
        string? chatOutputSchemaJson = null,
        string? judgeInstruction = null,
        string? judgeSchemaJson = null,
        string? leadInstruction = null,
        string? leadSchemaJson = null)
    {
        Id = id;
        DisplayName = displayName;
        Prompts = prompts;
        ClassificationSchema = classificationSchema;
        ChatOutputSchemaJson = NormalizeOptionalJson(chatOutputSchemaJson);
        JudgeInstruction = NormalizeOptionalText(judgeInstruction);
        JudgeSchemaJson = NormalizeOptionalJson(judgeSchemaJson);
        LeadInstruction = NormalizeOptionalText(leadInstruction);
        LeadSchemaJson = NormalizeOptionalJson(leadSchemaJson);
    }

    public void UpdateJudgeAndLeadPrompts(string judgePrompt, string leadPrompt)
    {
        if (string.IsNullOrWhiteSpace(judgePrompt))
            throw new ArgumentException("Judge prompt cannot be null or whitespace.", nameof(judgePrompt));

        if (string.IsNullOrWhiteSpace(leadPrompt))
            throw new ArgumentException("Lead prompt cannot be null or whitespace.", nameof(leadPrompt));

        Prompts = new PromptSections(Prompts.System, judgePrompt, leadPrompt);
    }

    public void UpdateAll(
        string displayName,
        string systemPrompt,
        string judgePrompt,
        string leadPrompt,
        ClassificationSchema classificationSchema)
    {
        DisplayName = displayName;
        UpdateJudgeAndLeadPrompts(judgePrompt, leadPrompt);
        Prompts = new PromptSections(systemPrompt, Prompts.Judge, Prompts.Lead);
        ClassificationSchema = classificationSchema;
    }

    /// <summary>Updates persona including optional prompt extension fields; pass <c>null</c> to leave a field unchanged.</summary>
    public void UpdateAll(
        string displayName,
        string systemPrompt,
        string judgePrompt,
        string leadPrompt,
        ClassificationSchema classificationSchema,
        string? chatOutputSchemaJson,
        string? judgeInstruction,
        string? judgeSchemaJson,
        string? leadInstruction,
        string? leadSchemaJson)
    {
        UpdateAll(displayName, systemPrompt, judgePrompt, leadPrompt, classificationSchema);
        if (chatOutputSchemaJson is not null)
            ChatOutputSchemaJson = NormalizeOptionalJson(chatOutputSchemaJson);
        if (judgeInstruction is not null)
            JudgeInstruction = NormalizeOptionalText(judgeInstruction);
        if (judgeSchemaJson is not null)
            JudgeSchemaJson = NormalizeOptionalJson(judgeSchemaJson);
        if (leadInstruction is not null)
            LeadInstruction = NormalizeOptionalText(leadInstruction);
        if (leadSchemaJson is not null)
            LeadSchemaJson = NormalizeOptionalJson(leadSchemaJson);
    }

    /// <summary>Replaces extension fields (including clearing when empty string is passed).</summary>
    public void ReplacePromptExtensionFields(
        string? chatOutputSchemaJson,
        string? judgeInstruction,
        string? judgeSchemaJson,
        string? leadInstruction,
        string? leadSchemaJson)
    {
        ChatOutputSchemaJson = NormalizeOptionalJson(chatOutputSchemaJson);
        JudgeInstruction = NormalizeOptionalText(judgeInstruction);
        JudgeSchemaJson = NormalizeOptionalJson(judgeSchemaJson);
        LeadInstruction = NormalizeOptionalText(leadInstruction);
        LeadSchemaJson = NormalizeOptionalJson(leadSchemaJson);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        if (value is null)
            return null;
        var t = value.Trim();
        return t.Length == 0 ? null : t;
    }

    private static string? NormalizeOptionalJson(string? value)
    {
        if (value is null)
            return null;
        var t = value.Trim();
        if (t.Length == 0 || t == "{}" || string.Equals(t, "null", StringComparison.OrdinalIgnoreCase))
            return null;
        return t;
    }
}
