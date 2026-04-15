using AiEmployee.Application.Dtos.Personas;
using AiEmployee.Application.Personas;

namespace AiEmployee.UnitTests;

public sealed class PersonaRequestValidatorExtensionsTests
{
    private static CreatePersonaRequest ValidBaseRequest() =>
        new()
        {
            DisplayName = "Test",
            Prompts = new PromptSectionsDto
            {
                System = "You are an AI assistant helping users.",
                Judge = "Task: analyze. Return json. {{input}}",
                Lead = "Return { \"x\": 1 }. {{goal}} {{experience}}",
            },
            ClassificationSchema = new ClassificationSchemaDto
            {
                UserTypes = new List<string> { "a" },
                Intents = new List<string> { "b" },
                Potentials = new List<string> { "c" },
            },
        };

    [Fact]
    public void Validate_rejects_invalid_extension_json()
    {
        var r = ValidBaseRequest();
        r.PromptExtensions = new PersonaPromptExtensionsDto
        {
            JudgeSchemaJson = "{not json",
        };

        var ex = Assert.Throws<PersonaValidationException>(() => PersonaRequestValidator.Validate(r));
        Assert.Contains(ex.Errors, e => e.Contains("promptExtensions.judgeSchemaJson", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_accepts_valid_extension_json()
    {
        var r = ValidBaseRequest();
        r.PromptExtensions = new PersonaPromptExtensionsDto
        {
            ChatOutputSchemaJson = """{"type":"object"}""",
            JudgeSchemaJson = """{"type":"object"}""",
        };

        PersonaRequestValidator.Validate(r);
    }
}
