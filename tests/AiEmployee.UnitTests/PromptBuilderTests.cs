using AiEmployee.Application.Prompting;
using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace AiEmployee.UnitTests;

public class PromptBuilderTests
{
    [Fact]
    public void BuildFullJudgePrompt_MissingInputPlaceholder_ThrowsInvalidOperationException()
    {
        var builder = new PromptBuilder(NullLogger<PromptBuilder>.Instance);
        var basePersona = JudgeBotDefaults.CreatePersona();
        var badPersona = new Persona(
            basePersona.Id,
            basePersona.DisplayName,
            new PromptSections(
                basePersona.Prompts.System,
                "Judge without the required token.",
                basePersona.Prompts.Lead),
            basePersona.ClassificationSchema);

        var conversation = new Conversation("c1");
        conversation.AddMessage(new Message("c1", "u1", "hello"));

        var ex = Assert.Throws<InvalidOperationException>(() => builder.BuildFullJudgePrompt(
            conversation,
            JudgeBotDefaults.CreateBehavior(),
            badPersona,
            JudgeBotDefaults.CreateJudgeTranscriptWrapperTemplate()));

        Assert.Contains("{{input}}", ex.Message);
        Assert.Contains(basePersona.Id.ToString(), ex.Message);
    }

    [Fact]
    public void BuildFullJudgePrompt_MissingTranscriptPlaceholder_ThrowsInvalidOperationException()
    {
        var builder = new PromptBuilder(NullLogger<PromptBuilder>.Instance);
        var badWrapper = new PromptTemplate(
            JudgeBotDefaults.JudgeTranscriptWrapperTemplateId,
            JudgeBotDefaults.JudgeTranscriptWrapperTemplateName,
            "No transcript token here.");

        var conversation = new Conversation("c1");
        conversation.AddMessage(new Message("c1", "u1", "hello"));

        var ex = Assert.Throws<InvalidOperationException>(() => builder.BuildFullJudgePrompt(
            conversation,
            JudgeBotDefaults.CreateBehavior(),
            JudgeBotDefaults.CreatePersona(),
            badWrapper));

        Assert.Equal(PromptTokens.JudgeWrapperMissingTranscriptMessage, ex.Message);
    }

    [Fact]
    public void BuildFullJudgePrompt_InjectsTranscriptIntoJudgeTemplate()
    {
        var builder = new PromptBuilder(NullLogger<PromptBuilder>.Instance);
        var conversation = new Conversation("c1");
        conversation.AddMessage(new Message("c1", "u1", "unique-transcript-line-xyz"));

        var built = builder.BuildFullJudgePrompt(
            conversation,
            JudgeBotDefaults.CreateBehavior(),
            JudgeBotDefaults.CreatePersona(),
            JudgeBotDefaults.CreateJudgeTranscriptWrapperTemplate());

        Assert.Contains("unique-transcript-line-xyz", built.Prompt);
        Assert.Equal(PromptHashing.ComputeSha256(built.Prompt), built.PromptHash);
    }
}
