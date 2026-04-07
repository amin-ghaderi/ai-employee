using System.Text.Json;
using AiEmployee.Domain.BotConfiguration;
using Microsoft.Extensions.Logging;

namespace AiEmployee.Application.Prompting;

public sealed class BehaviorPromptMapper
{
    private readonly ILogger<BehaviorPromptMapper> _logger;

    public BehaviorPromptMapper(ILogger<BehaviorPromptMapper> logger)
    {
        _logger = logger;
    }

    public string BuildJudgePrompt(Persona persona, Behavior behavior)
    {
        ArgumentNullException.ThrowIfNull(persona);
        ArgumentNullException.ThrowIfNull(behavior);

        var instruction = behavior.JudgeInstruction;
        var schema = ParseSchema(behavior.JudgeSchemaJson);
        if (!HasJudgeOverride(behavior))
        {
            _logger.LogInformation("PromptSource = Persona (Judge)");
            return persona.Prompts.Judge;
        }

        var combined = CombineInstructionAndSchema(instruction, schema);
        WarnIfMissingJudgeToken(combined);
        _logger.LogInformation("PromptSource = Behavior (Judge)");
        return combined;
    }

    public string BuildLeadPrompt(Persona persona, Behavior behavior)
    {
        ArgumentNullException.ThrowIfNull(persona);
        ArgumentNullException.ThrowIfNull(behavior);

        var instruction = behavior.LeadInstruction;
        var schema = ParseSchema(behavior.LeadSchemaJson);
        if (string.IsNullOrWhiteSpace(instruction) && IsSchemaEmpty(schema))
        {
            _logger.LogInformation("PromptSource = Persona (Lead)");
            return persona.Prompts.Lead;
        }

        var combined = CombineInstructionAndSchema(instruction, schema);
        WarnIfMissingLeadTokens(combined);
        _logger.LogInformation("PromptSource = Behavior (Lead)");
        return combined;
    }

    public static bool HasJudgeOverride(Behavior behavior)
    {
        ArgumentNullException.ThrowIfNull(behavior);
        return
            !string.IsNullOrWhiteSpace(behavior.JudgeInstruction)
            || !string.IsNullOrWhiteSpace(behavior.JudgeSchemaJson);
    }

    public static string GetJudgePromptSource(Behavior behavior)
    {
        return HasJudgeOverride(behavior) ? "Behavior" : "Persona";
    }

    public static bool HasLeadOverride(Behavior behavior)
    {
        ArgumentNullException.ThrowIfNull(behavior);
        return
            !string.IsNullOrWhiteSpace(behavior.LeadInstruction)
            || !string.IsNullOrWhiteSpace(behavior.LeadSchemaJson);
    }

    public static string GetLeadPromptSource(Behavior behavior)
    {
        return HasLeadOverride(behavior) ? "Behavior" : "Persona";
    }

    public static object? ParseSchema(string? schemaJson)
    {
        if (string.IsNullOrWhiteSpace(schemaJson))
            return null;

        var trimmed = schemaJson.Trim();
        if (trimmed == "{}" || trimmed == "null")
            return null;

        try
        {
            return JsonSerializer.Deserialize<object>(schemaJson);
        }
        catch
        {
            return null;
        }
    }

    private static string CombineInstructionAndSchema(string? instruction, object? schema)
    {
        var trimmedInstruction = (instruction ?? string.Empty).Trim();
        var schemaText = SerializeSchema(schema);

        if (string.IsNullOrWhiteSpace(schemaText))
            return trimmedInstruction;

        if (trimmedInstruction.Contains("Return JSON", StringComparison.OrdinalIgnoreCase))
            return $"{trimmedInstruction}{Environment.NewLine}{Environment.NewLine}{schemaText}";

        if (string.IsNullOrWhiteSpace(trimmedInstruction))
            return $"Return JSON:{Environment.NewLine}{schemaText}";

        return $"{trimmedInstruction}{Environment.NewLine}{Environment.NewLine}Return JSON:{Environment.NewLine}{schemaText}";
    }

    private void WarnIfMissingJudgeToken(string prompt)
    {
        if (!prompt.Contains(PromptTokens.Input, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Behavior judge prompt override is missing placeholder {Placeholder}.",
                PromptTokens.Input);
        }
    }

    private void WarnIfMissingLeadTokens(string prompt)
    {
        if (!prompt.Contains(PromptTokens.Goal, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Behavior lead prompt override is missing placeholder {Placeholder}.",
                PromptTokens.Goal);
        }

        if (!prompt.Contains(PromptTokens.Experience, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Behavior lead prompt override is missing placeholder {Placeholder}.",
                PromptTokens.Experience);
        }
    }

    private static bool IsSchemaEmpty(object? schema)
    {
        if (schema is null)
            return true;

        if (schema is string s)
            return string.IsNullOrWhiteSpace(s);

        if (schema is JsonElement el)
        {
            return el.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
                || (el.ValueKind == JsonValueKind.Array && el.GetArrayLength() == 0)
                || (el.ValueKind == JsonValueKind.Object && !el.EnumerateObject().Any());
        }

        return false;
    }

    private static string SerializeSchema(object? schema)
    {
        if (schema is null)
            return string.Empty;

        if (schema is string s)
        {
            var trimmed = s.Trim();
            return (trimmed == "null" || trimmed == "{}") ? string.Empty : trimmed;
        }

        if (schema is JsonElement el)
        {
            return JsonSerializer.Serialize(el, new JsonSerializerOptions { WriteIndented = true });
        }

        return JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = true });
    }

}
