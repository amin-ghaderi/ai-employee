using AiEmployee.Application.Dtos.Personas;
using AiEmployee.Application.Personas;

namespace AiEmployee.UnitTests;

public class PersonaRequestValidatorTests
{
    [Fact]
    public void Validate_CreatePersona_JudgeMissingInputPlaceholder_IncludesExpectedError()
    {
        var request = ValidCreateRequest();
        request.Prompts.Judge = MinimalValidJudge().Replace("{{input}}", "", StringComparison.Ordinal);

        var ex = Assert.Throws<PersonaValidationException>(() => PersonaRequestValidator.Validate(request));

        Assert.Contains(
            "Judge prompt must contain '{{input}}' placeholder for transcript injection.",
            ex.Errors);
    }

    [Fact]
    public void Validate_UpdatePersona_JudgeMissingInputPlaceholder_IncludesExpectedError()
    {
        var request = ValidUpdateRequest();
        request.Prompts.Judge = "No placeholder but task analyze decide determine json { ";

        var ex = Assert.Throws<PersonaValidationException>(() => PersonaRequestValidator.Validate(request));

        Assert.Contains(
            "Judge prompt must contain '{{input}}' placeholder for transcript injection.",
            ex.Errors);
    }

    [Fact]
    public void Validate_CreatePersona_ValidPrompts_DoesNotThrow()
    {
        var request = ValidCreateRequest();
        PersonaRequestValidator.Validate(request);
    }

    [Fact]
    public void ValidateJudgeAndLeadForBotConfigUpdate_InvalidJudge_ThrowsPersonaValidationException()
    {
        var ex = Assert.Throws<PersonaValidationException>(() =>
            PersonaRequestValidator.ValidateJudgeAndLeadForBotConfigUpdate(
                "Short judge without rules.",
                MinimalValidLead()));

        Assert.Contains(
            "Judge prompt must contain '{{input}}' placeholder for transcript injection.",
            ex.Errors);
    }

    [Fact]
    public void ValidateJudgeAndLeadForBotConfigUpdate_ValidPair_DoesNotThrow()
    {
        PersonaRequestValidator.ValidateJudgeAndLeadForBotConfigUpdate(MinimalValidJudge(), MinimalValidLead());
    }

    [Fact]
    public void Validate_CreatePersona_CollectsMultipleJudgeErrors()
    {
        var request = ValidCreateRequest();
        request.Prompts.Judge = "plain";

        var ex = Assert.Throws<PersonaValidationException>(() => PersonaRequestValidator.Validate(request));

        Assert.True(ex.Errors.Count >= 3);
        Assert.Contains(ex.Errors, e => e.Contains("{{input}}", StringComparison.Ordinal));
        Assert.Contains(ex.Errors, e => e.Contains("analyze", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ex.Errors, e => e.Contains("json", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_CreatePersona_LeadMissingPlaceholders_IncludesExpectedError()
    {
        var request = ValidCreateRequest();
        request.Prompts.Lead = "Your task is to classify. Return {\"x\":\"y\"}";

        var ex = Assert.Throws<PersonaValidationException>(() => PersonaRequestValidator.Validate(request));

        Assert.Contains(
            "Lead prompt must contain '{{goal}}' and '{{experience}}' placeholders.",
            ex.Errors);
    }

    [Fact]
    public void Validate_CreatePersona_SystemMissingBehavioralHint_IncludesExpectedError()
    {
        var request = ValidCreateRequest();
        request.Prompts.System = "Only generic text without required hints.";

        var ex = Assert.Throws<PersonaValidationException>(() => PersonaRequestValidator.Validate(request));

        Assert.Contains(
            ex.Errors,
            e => e.StartsWith("System prompt must describe assistant behavior", StringComparison.Ordinal));
    }

    private static CreatePersonaRequest ValidCreateRequest() =>
        new()
        {
            DisplayName = "Test",
            Prompts = new PromptSectionsDto
            {
                System = "You are an AI assistant for unit tests.",
                Judge = MinimalValidJudge(),
                Lead = MinimalValidLead(),
            },
            ClassificationSchema = new ClassificationSchemaDto
            {
                UserTypes = new[] { "a" },
                Intents = new[] { "b" },
                Potentials = new[] { "c" },
            },
        };

    private static UpdatePersonaRequest ValidUpdateRequest() =>
        new()
        {
            DisplayName = "Test",
            Prompts = new PromptSectionsDto
            {
                System = "You are an assistant for unit tests.",
                Judge = MinimalValidJudge(),
                Lead = MinimalValidLead(),
            },
            ClassificationSchema = new ClassificationSchemaDto
            {
                UserTypes = new[] { "a" },
                Intents = new[] { "b" },
                Potentials = new[] { "c" },
            },
        };

    private static string MinimalValidJudge() =>
        """
        Your task is to analyze and decide. Reply with JSON only:
        {
          "winner": "...",
          "reason": "..."
        }
        {{input}}
        """;

    private static string MinimalValidLead() =>
        """
        User goal: {{goal}}, experience: {{experience}}
        Return JSON: { "user_type": "...", "intent": "...", "potential": "..." }
        """;
}
