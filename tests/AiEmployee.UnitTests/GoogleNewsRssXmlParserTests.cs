using AiEmployee.Infrastructure.News;

namespace AiEmployee.UnitTests;

public class GoogleNewsRssXmlParserTests
{
    private const string SampleRss = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
          <channel>
            <item>
              <title>Alpha &amp; Beta - ExampleNews</title>
              <link>https://example.com/a</link>
            </item>
            <item>
              <title>Second headline here</title>
            </item>
            <item>
              <title>Third</title>
            </item>
          </channel>
        </rss>
        """;

    [Fact]
    public void ParseItems_ReturnsTitles_DecodesEntities_RespectsMax()
    {
        var items = GoogleNewsRssXmlParser.ParseItems(SampleRss, maxResults: 2, maxTitleChars: 200);

        Assert.Equal(2, items.Count);
        Assert.Equal("Alpha & Beta - ExampleNews", items[0].Title);
        Assert.Equal("https://example.com/a", items[0].Link);
        Assert.Equal("Second headline here", items[1].Title);
        Assert.Null(items[1].Link);
    }

    [Fact]
    public void ParseItems_TruncatesTitle_WhenMaxTitleCharsSmall()
    {
        var items = GoogleNewsRssXmlParser.ParseItems(SampleRss, maxResults: 1, maxTitleChars: 6);

        Assert.Single(items);
        Assert.Equal("Alpha …", items[0].Title);
    }

    [Fact]
    public void ParseItems_InvalidXml_ReturnsEmpty()
    {
        var items = GoogleNewsRssXmlParser.ParseItems("not xml", maxResults: 5, maxTitleChars: 100);
        Assert.Empty(items);
    }
}
