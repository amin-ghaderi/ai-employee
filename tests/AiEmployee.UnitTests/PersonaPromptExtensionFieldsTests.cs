using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.UnitTests;

public sealed class PersonaPromptExtensionFieldsTests
{
    [Fact]
    public void Constructor_normalizes_empty_json_and_whitespace_to_null()
    {
        var p = new Persona(
            Guid.NewGuid(),
            "Test",
            new PromptSections("sys", "judge", "lead"),
            new ClassificationSchema([], [], []),
            chatOutputSchemaJson: "  {}  ",
            judgeInstruction: "  ",
            judgeSchemaJson: "null",
            leadInstruction: "\t",
            leadSchemaJson: "{}");

        Assert.Null(p.ChatOutputSchemaJson);
        Assert.Null(p.JudgeInstruction);
        Assert.Null(p.JudgeSchemaJson);
        Assert.Null(p.LeadInstruction);
        Assert.Null(p.LeadSchemaJson);
    }

    [Fact]
    public void ReplacePromptExtensionFields_can_set_and_clear()
    {
        var p = new Persona(
            Guid.NewGuid(),
            "Test",
            new PromptSections("sys", "judge", "lead"),
            new ClassificationSchema([], [], []),
            judgeInstruction: "keep me");

        p.ReplacePromptExtensionFields(null, null, null, null, null);
        Assert.Null(p.JudgeInstruction);
    }
}
