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

    /// <summary>Builds the general chat system block: chat instruction plus optional output schema appendix.</summary>
    public static string BuildChatSystemContent(Persona persona)
    {
        ArgumentNullException.ThrowIfNull(persona);
        var system = (persona.Prompts.System ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(persona.ChatOutputSchemaJson))
            return system;

        var schema = ParseSchema(persona.ChatOutputSchemaJson);
        if (IsSchemaEmpty(schema))
            return system;

        var schemaText = SerializeSchema(schema);
        if (string.IsNullOrWhiteSpace(schemaText))
            return system;

        if (string.IsNullOrWhiteSpace(system))
        {
            return
                "You are a helpful assistant. Your reply MUST be valid JSON conforming to the following schema:"
                + Environment.NewLine
                + schemaText;
        }

        return
            system
            + Environment.NewLine
            + Environment.NewLine
            + "Your reply MUST be valid JSON conforming to the following schema:"
            + Environment.NewLine
            + schemaText;
    }

    public string BuildJudgePrompt(Persona persona)
    {
        ArgumentNullException.ThrowIfNull(persona);

        if (HasPersonaJudgeExtensions(persona))
        {
            var combined = CombineInstructionAndSchemaStatic(persona.JudgeInstruction, ParseSchema(persona.JudgeSchemaJson));
            if (!string.IsNullOrWhiteSpace(combined))
            {
                WarnIfMissingJudgeToken(combined, "Persona-extensions");
                _logger.LogInformation("PromptSource = Persona-extensions (Judge)");
                return combined;
            }

            _logger.LogWarning(
                "Persona judge extensions were present but produced an empty combined prompt; falling back to template.");
        }

        _logger.LogInformation("PromptSource = Persona-template (Judge)");
        return persona.Prompts.Judge;
    }

    public string BuildLeadPrompt(Persona persona)
    {
        ArgumentNullException.ThrowIfNull(persona);

        if (HasPersonaLeadExtensions(persona))
        {
            var combined = CombineInstructionAndSchemaStatic(persona.LeadInstruction, ParseSchema(persona.LeadSchemaJson));
            if (!string.IsNullOrWhiteSpace(combined))
            {
                WarnIfMissingLeadTokens(combined, "Persona-extensions");
                _logger.LogInformation("PromptSource = Persona-extensions (Lead)");
                return combined;
            }

            _logger.LogWarning(
                "Persona lead extensions were present but produced an empty combined prompt; falling back to template.");
        }

        _logger.LogInformation("PromptSource = Persona-template (Lead)");
        return persona.Prompts.Lead;
    }

    public static bool HasPersonaJudgeExtensions(Persona persona)
    {
        ArgumentNullException.ThrowIfNull(persona);
        if (!string.IsNullOrWhiteSpace(persona.JudgeInstruction))
            return true;
        return !IsTrivialSchemaJson(persona.JudgeSchemaJson);
    }

    public static bool HasPersonaLeadExtensions(Persona persona)
    {
        ArgumentNullException.ThrowIfNull(persona);
        if (!string.IsNullOrWhiteSpace(persona.LeadInstruction))
            return true;
        return !IsTrivialSchemaJson(persona.LeadSchemaJson);
    }

    /// <summary>Effective prompt origin label for admin/debug (Persona-extensions vs Persona-template).</summary>
    public static string GetJudgePromptSource(Persona persona)
    {
        ArgumentNullException.ThrowIfNull(persona);
        if (HasPersonaJudgeExtensions(persona))
        {
            var combined = CombineInstructionAndSchemaStatic(persona.JudgeInstruction, ParseSchema(persona.JudgeSchemaJson));
            if (!string.IsNullOrWhiteSpace(combined))
                return "Persona-extensions";
        }

        return "Persona-template";
    }

    public static string GetLeadPromptSource(Persona persona)
    {
        ArgumentNullException.ThrowIfNull(persona);
        if (HasPersonaLeadExtensions(persona))
        {
            var combined = CombineInstructionAndSchemaStatic(persona.LeadInstruction, ParseSchema(persona.LeadSchemaJson));
            if (!string.IsNullOrWhiteSpace(combined))
                return "Persona-extensions";
        }

        return "Persona-template";
    }

    public static string? GetEffectiveJudgeSchemaJson(Persona persona)
    {
        ArgumentNullException.ThrowIfNull(persona);
        return persona.JudgeSchemaJson;
    }

    public static string? GetEffectiveLeadSchemaJson(Persona persona)
    {
        ArgumentNullException.ThrowIfNull(persona);
        return persona.LeadSchemaJson;
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

    private static bool IsTrivialSchemaJson(string? schemaJson)
    {
        if (string.IsNullOrWhiteSpace(schemaJson))
            return true;
        var t = schemaJson.Trim();
        return t == "{}" || t.Equals("null", StringComparison.OrdinalIgnoreCase);
    }

    private static string CombineInstructionAndSchemaStatic(string? instruction, object? schema)
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

    private void WarnIfMissingJudgeToken(string prompt, string source)
    {
        if (!prompt.Contains(PromptTokens.Input, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "{Source} judge prompt is missing placeholder {Placeholder}.",
                source,
                PromptTokens.Input);
        }
    }

    private void WarnIfMissingLeadTokens(string prompt, string source)
    {
        if (!prompt.Contains(PromptTokens.Goal, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "{Source} lead prompt is missing placeholder {Placeholder}.",
                source,
                PromptTokens.Goal);
        }

        if (!prompt.Contains(PromptTokens.Experience, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "{Source} lead prompt is missing placeholder {Placeholder}.",
                source,
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
