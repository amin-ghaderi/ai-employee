using System.Text.Json;
using AiEmployee.Application.Dtos.Personas;
using AiEmployee.Application.Prompting;

namespace AiEmployee.Application.Personas;

public static class PersonaRequestValidator
{
    private const string JudgeMissingInputPlaceholderMessage =
        "Judge prompt must contain '{{input}}' placeholder for transcript injection.";

    private const string JudgeMissingInstructionKeywordMessage =
        "Judge prompt must include an instruction to analyze, decide, or determine an outcome (e.g. the words: task, analyze, decide, determine).";

    private const string JudgeMissingJsonHintMessage =
        "Judge prompt must hint at JSON output (include the word \"json\" or a '{' character showing the expected response shape).";

    private const string LeadMissingPlaceholdersMessage =
        "Lead prompt must contain '{{goal}}' and '{{experience}}' placeholders.";

    private const string LeadMissingJsonBraceMessage =
        "Lead prompt must include a JSON output hint (a '{' character in the template).";

    private const string SystemMissingBehavioralHintMessage =
        "System prompt must describe assistant behavior (e.g. mention you are an assistant, AI, or that you assist users).";

    private static readonly string[] JudgeInstructionKeywords =
        ["task", "analyze", "decide", "determine"];

    private static readonly string[] SystemBehavioralKeywords =
        ["you are", "assistant", "ai", "assist"];

    /// <summary>
    /// Validates judge and lead prompts for <see cref="Interfaces.IBotConfigurationCommand.UpdatePromptsAsync"/> using the same lint rules as full persona create/update.
    /// </summary>
    public static void ValidateJudgeAndLeadForBotConfigUpdate(string judgePrompt, string leadPrompt)
    {
        var errors = new List<string>();
        if (!string.IsNullOrWhiteSpace(judgePrompt))
            AddJudgePromptLintErrors(judgePrompt, errors);
        if (!string.IsNullOrWhiteSpace(leadPrompt))
            AddLeadPromptLintErrors(leadPrompt, errors);

        if (errors.Count > 0)
            throw new PersonaValidationException(errors);
    }

    public static void Validate(CreatePersonaRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = CollectErrors(
            request.DisplayName,
            request.Prompts,
            request.ClassificationSchema,
            request.PromptExtensions);
        if (errors.Count > 0)
            throw new PersonaValidationException(errors);
    }

    public static void Validate(UpdatePersonaRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = CollectErrors(
            request.DisplayName,
            request.Prompts,
            request.ClassificationSchema,
            request.PromptExtensions);
        if (errors.Count > 0)
            throw new PersonaValidationException(errors);
    }

    private static List<string> CollectErrors(
        string displayName,
        PromptSectionsDto? prompts,
        ClassificationSchemaDto? classificationSchema,
        PersonaPromptExtensionsDto? promptExtensions)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(displayName))
            errors.Add("displayName must not be null or empty.");

        if (prompts is null)
        {
            errors.Add("prompts is required.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(prompts.System))
                errors.Add("prompts.system must not be null or empty.");
            else
                AddSystemPromptLintErrors(prompts.System, errors);

            if (string.IsNullOrWhiteSpace(prompts.Judge))
                errors.Add("prompts.judge must not be null or empty.");
            else
                AddJudgePromptLintErrors(prompts.Judge, errors);

            if (string.IsNullOrWhiteSpace(prompts.Lead))
                errors.Add("prompts.lead must not be null or empty.");
            else
                AddLeadPromptLintErrors(prompts.Lead, errors);
        }

        if (classificationSchema is null)
        {
            errors.Add("classificationSchema is required.");
            return errors;
        }

        ValidateStringList(classificationSchema.UserTypes, "classificationSchema.userTypes", errors);
        ValidateStringList(classificationSchema.Intents, "classificationSchema.intents", errors);
        ValidateStringList(classificationSchema.Potentials, "classificationSchema.potentials", errors);

        AddPromptExtensionJsonLintErrors(promptExtensions, errors);

        return errors;
    }

    private static void AddPromptExtensionJsonLintErrors(PersonaPromptExtensionsDto? extensions, List<string> errors)
    {
        if (extensions is null)
            return;

        AddJsonIfNonEmpty(extensions.ChatOutputSchemaJson, "promptExtensions.chatOutputSchemaJson", errors);
        AddJsonIfNonEmpty(extensions.JudgeSchemaJson, "promptExtensions.judgeSchemaJson", errors);
        AddJsonIfNonEmpty(extensions.LeadSchemaJson, "promptExtensions.leadSchemaJson", errors);
    }

    private static void AddJsonIfNonEmpty(string? raw, string fieldName, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return;

        var trimmed = raw.Trim();
        if (trimmed == "{}" || trimmed.Equals("null", StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            JsonDocument.Parse(trimmed);
        }
        catch (JsonException)
        {
            errors.Add($"{fieldName} must be valid JSON.");
        }
    }

    private static void AddSystemPromptLintErrors(string system, List<string> errors)
    {
        if (!ContainsAnyKeyword(system, SystemBehavioralKeywords))
            errors.Add(SystemMissingBehavioralHintMessage);
    }

    private static void AddJudgePromptLintErrors(string judge, List<string> errors)
    {
        if (!judge.Contains(PromptTokens.Input, StringComparison.Ordinal))
            errors.Add(JudgeMissingInputPlaceholderMessage);

        if (!ContainsAnyKeyword(judge, JudgeInstructionKeywords))
            errors.Add(JudgeMissingInstructionKeywordMessage);

        if (!judge.Contains('{') && !judge.Contains("json", StringComparison.OrdinalIgnoreCase))
            errors.Add(JudgeMissingJsonHintMessage);
    }

    private static void AddLeadPromptLintErrors(string lead, List<string> errors)
    {
        var hasGoal = lead.Contains(PromptTokens.Goal, StringComparison.Ordinal);
        var hasExperience = lead.Contains(PromptTokens.Experience, StringComparison.Ordinal);
        if (!hasGoal || !hasExperience)
            errors.Add(LeadMissingPlaceholdersMessage);

        if (!lead.Contains('{'))
            errors.Add(LeadMissingJsonBraceMessage);
    }

    private static bool ContainsAnyKeyword(string text, string[] keywords)
    {
        foreach (var kw in keywords)
        {
            if (text.Contains(kw, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static void ValidateStringList(
        IReadOnlyList<string>? values,
        string name,
        List<string> errors)
    {
        if (values is null)
        {
            errors.Add($"{name} must not be null.");
            return;
        }

        for (var i = 0; i < values.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(values[i]))
                errors.Add($"{name}[{i}] must not be null or empty.");
        }
    }
}
