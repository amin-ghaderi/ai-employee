using AiEmployee.Application.Options;

namespace AiEmployee.Application.News;

public static class LiveNewsTriggerEvaluator
{
    private static readonly string[] DefaultKeywords =
    {
        "news", "headlines", "breaking", "latest", "today", "current events",
    };

    public static bool ShouldFetchLiveNews(string? userInput, LiveNewsOptions options)
    {
        if (!options.Enabled || string.IsNullOrWhiteSpace(userInput))
            return false;

        var keywords = options.TriggerKeywords is { Length: > 0 }
            ? options.TriggerKeywords
            : DefaultKeywords;

        foreach (var kw in keywords)
        {
            if (string.IsNullOrWhiteSpace(kw))
                continue;
            if (userInput.Contains(kw.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
