using System.Net.Http;
using AiEmployee.Application.Interfaces;
using AiEmployee.Application.News;
using AiEmployee.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiEmployee.Infrastructure.News;

public sealed class GoogleNewsRssService : INewsSearchService
{
    public const string HttpClientName = "GoogleNews";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<LiveNewsOptions> _options;
    private readonly ILogger<GoogleNewsRssService> _logger;

    public GoogleNewsRssService(
        IHttpClientFactory httpClientFactory,
        IOptions<LiveNewsOptions> options,
        ILogger<GoogleNewsRssService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NewsHeadline>> GetHeadlinesAsync(
        string searchQuery,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        var q = (searchQuery ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(q))
            return Array.Empty<NewsHeadline>();

        var opts = _options.Value;
        var take = Math.Clamp(maxResults, 1, 5);
        var url = BuildRssUrl(q, opts.Language, opts.Region);

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Google News RSS request failed | status={StatusCode} url={Url}",
                    (int)response.StatusCode,
                    url);
                return Array.Empty<NewsHeadline>();
            }

            var xml = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var headlines = GoogleNewsRssXmlParser.ParseItems(xml, take, opts.MaxTitleChars);
            return headlines;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Google News RSS request error | url={Url}", url);
            return Array.Empty<NewsHeadline>();
        }
    }

    public static string BuildRssUrl(string query, string language, string region)
    {
        var hl = string.IsNullOrWhiteSpace(language) ? "en" : language.Trim();
        var gl = string.IsNullOrWhiteSpace(region) ? "US" : region.Trim().ToUpperInvariant();
        var ceid = $"{gl}:{hl}";
        return
            "https://news.google.com/rss/search?q="
            + Uri.EscapeDataString(query)
            + "&hl="
            + Uri.EscapeDataString(hl)
            + "&gl="
            + Uri.EscapeDataString(gl)
            + "&ceid="
            + Uri.EscapeDataString(ceid);
    }
}
