using AiEmployee.Application.Prompting;
using AiEmployee.Domain.BotConfiguration;
using Microsoft.Extensions.Logging.Abstractions;

namespace AiEmployee.UnitTests;

public sealed class BehaviorPromptMapperTests
{
    private static BehaviorPromptMapper Mapper => new(NullLogger<BehaviorPromptMapper>.Instance);

    [Fact]
    public void BuildJudgePrompt_prefers_persona_extensions_over_template()
    {
        var persona = new Persona(
            Guid.NewGuid(),
            "P",
            new PromptSections("sys", "TEMPLATE_JUDGE", "lead"),
            new ClassificationSchema([], [], []),
            judgeInstruction: "Decide winner. Use {{input}}",
            judgeSchemaJson: """{"type":"object"}""");

        var prompt = Mapper.BuildJudgePrompt(persona);

        Assert.Contains("Decide winner", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("TEMPLATE_JUDGE", prompt, StringComparison.Ordinal);
        Assert.Equal("Persona-extensions", BehaviorPromptMapper.GetJudgePromptSource(persona));
    }

    [Fact]
    public void BuildJudgePrompt_falls_back_to_persona_template_when_extensions_empty()
    {
        var persona = new Persona(
            Guid.NewGuid(),
            "P",
            new PromptSections("sys", "TEMPLATE_JUDGE_FALLBACK", "lead"),
            new ClassificationSchema([], [], []));

        var prompt = Mapper.BuildJudgePrompt(persona);

        Assert.Contains("TEMPLATE_JUDGE_FALLBACK", prompt, StringComparison.Ordinal);
        Assert.Equal("Persona-template", BehaviorPromptMapper.GetJudgePromptSource(persona));
    }

    [Fact]
    public void BuildChatSystemContent_appends_schema_when_chat_output_schema_set()
    {
        var persona = new Persona(
            Guid.NewGuid(),
            "P",
            new PromptSections("You are an assistant.", "j", "l"),
            new ClassificationSchema([], [], []),
            chatOutputSchemaJson: """{"type":"object","properties":{"ok":{"type":"boolean"}}}""");

        var block = BehaviorPromptMapper.BuildChatSystemContent(persona);

        Assert.Contains("You are an assistant", block, StringComparison.Ordinal);
        Assert.Contains("JSON", block, StringComparison.Ordinal);
        Assert.Contains("ok", block, StringComparison.Ordinal);
    }
}
