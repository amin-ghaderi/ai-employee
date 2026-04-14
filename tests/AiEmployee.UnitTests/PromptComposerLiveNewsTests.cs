using AiEmployee.Application.Prompting;
using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.UnitTests;

public class PromptComposerLiveNewsTests
{
    [Fact]
    public void BuildHybridChatPrompt_InsertsLiveNews_AfterSystem_BeforeRetrievedContext()
    {
        var composer = new PromptComposer();
        var persona = JudgeBotDefaults.CreatePersona();
        var liveNews = new[] { "- Headline A", "- Headline B" };
        var rag = new[] { "[1] (similarity 0.90) snippet" };
        var history = new[] { "User: hi" };

        var prompt = composer.BuildHybridChatPrompt(persona, rag, history, "What is new?", liveNews);

        var systemIdx = prompt.IndexOf("[SYSTEM]", StringComparison.Ordinal);
        var newsIdx = prompt.IndexOf("[LIVE NEWS]", StringComparison.Ordinal);
        var ragIdx = prompt.IndexOf("[RETRIEVED CONTEXT]", StringComparison.Ordinal);
        var histIdx = prompt.IndexOf("[RECENT CONVERSATION HISTORY]", StringComparison.Ordinal);
        var userIdx = prompt.IndexOf("[USER]", StringComparison.Ordinal);

        Assert.True(systemIdx >= 0);
        Assert.True(newsIdx > systemIdx);
        Assert.True(ragIdx > newsIdx);
        Assert.True(histIdx > ragIdx);
        Assert.True(userIdx > histIdx);
        Assert.Contains("Headline A", prompt);
        Assert.Contains("snippet", prompt);
    }

    [Fact]
    public void BuildHybridChatPrompt_OmitsLiveNews_WhenNullOrEmpty()
    {
        var composer = new PromptComposer();
        var persona = JudgeBotDefaults.CreatePersona();

        var a = composer.BuildHybridChatPrompt(persona, Array.Empty<string>(), Array.Empty<string>(), "Hi", null);
        var b = composer.BuildHybridChatPrompt(persona, Array.Empty<string>(), Array.Empty<string>(), "Hi", Array.Empty<string>());

        Assert.DoesNotContain("[LIVE NEWS]", a);
        Assert.DoesNotContain("[LIVE NEWS]", b);
    }
}
