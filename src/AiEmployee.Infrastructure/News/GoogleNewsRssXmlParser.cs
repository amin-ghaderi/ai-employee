using System.Net;
using System.Xml.Linq;
using AiEmployee.Application.News;

namespace AiEmployee.Infrastructure.News;

public static class GoogleNewsRssXmlParser
{
    public static IReadOnlyList<NewsHeadline> ParseItems(string xml, int maxResults, int maxTitleChars)
    {
        if (string.IsNullOrWhiteSpace(xml) || maxResults <= 0)
            return Array.Empty<NewsHeadline>();

        try
        {
            var doc = XDocument.Parse(xml);
            var items = doc
                .Descendants()
                .Where(e => e.Name.LocalName == "item")
                .Take(maxResults);

            var list = new List<NewsHeadline>(maxResults);
            foreach (var item in items)
            {
                var titleEl = item.Elements().FirstOrDefault(e => e.Name.LocalName == "title");
                var linkEl = item.Elements().FirstOrDefault(e => e.Name.LocalName == "link");
                var rawTitle = titleEl?.Value?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(rawTitle))
                    continue;

                var title = WebUtility.HtmlDecode(rawTitle);
                if (maxTitleChars > 0 && title.Length > maxTitleChars)
                    title = title[..maxTitleChars] + "…";

                var link = linkEl?.Value?.Trim();
                if (string.IsNullOrEmpty(link))
                    link = null;

                list.Add(new NewsHeadline { Title = title, Link = link });
            }

            return list;
        }
        catch
        {
            return Array.Empty<NewsHeadline>();
        }
    }
}
