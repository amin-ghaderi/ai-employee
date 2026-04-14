using AiEmployee.Infrastructure.News;

namespace AiEmployee.UnitTests;

public class GoogleNewsRssServiceTests
{
    [Fact]
    public void BuildRssUrl_EncodesQueryAndCeid()
    {
        var url = GoogleNewsRssService.BuildRssUrl("climate news", "en", "US");

        Assert.Contains("q=climate%20news", url);
        Assert.Contains("hl=en", url);
        Assert.Contains("gl=US", url);
        Assert.Contains("ceid=US%3Aen", url);
        Assert.StartsWith("https://news.google.com/rss/search?", url);
    }

    [Fact]
    public void BuildRssUrl_UsesDefaults_WhenLanguageOrRegionEmpty()
    {
        var url = GoogleNewsRssService.BuildRssUrl("x", "", "  ");

        Assert.Contains("hl=en", url);
        Assert.Contains("gl=US", url);
    }
}
