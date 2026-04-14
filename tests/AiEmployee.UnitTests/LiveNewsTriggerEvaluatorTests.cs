using AiEmployee.Application.News;
using AiEmployee.Application.Options;

namespace AiEmployee.UnitTests;

public class LiveNewsTriggerEvaluatorTests
{
    [Fact]
    public void ShouldFetchLiveNews_ReturnsFalse_WhenDisabled()
    {
        var opts = new LiveNewsOptions { Enabled = false };
        Assert.False(LiveNewsTriggerEvaluator.ShouldFetchLiveNews("any news today", opts));
    }

    [Fact]
    public void ShouldFetchLiveNews_ReturnsFalse_WhenInputEmpty()
    {
        var opts = new LiveNewsOptions { Enabled = true };
        Assert.False(LiveNewsTriggerEvaluator.ShouldFetchLiveNews(null, opts));
        Assert.False(LiveNewsTriggerEvaluator.ShouldFetchLiveNews("   ", opts));
    }

    [Fact]
    public void ShouldFetchLiveNews_UsesDefaultKeywords_WhenTriggerKeywordsEmpty()
    {
        var opts = new LiveNewsOptions { Enabled = true, TriggerKeywords = Array.Empty<string>() };
        Assert.True(LiveNewsTriggerEvaluator.ShouldFetchLiveNews("What is the latest news?", opts));
        Assert.False(LiveNewsTriggerEvaluator.ShouldFetchLiveNews("Hello there", opts));
    }

    [Fact]
    public void ShouldFetchLiveNews_UsesCustomKeywords()
    {
        var opts = new LiveNewsOptions
        {
            Enabled = true,
            TriggerKeywords = new[] { "stocks", "market" },
        };
        Assert.True(LiveNewsTriggerEvaluator.ShouldFetchLiveNews("How is the market?", opts));
        Assert.False(LiveNewsTriggerEvaluator.ShouldFetchLiveNews("latest news", opts));
    }
}
