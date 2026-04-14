using AiEmployee.Application.News;

namespace AiEmployee.Application.Interfaces;

public interface INewsSearchService
{
    /// <summary>
    /// Fetches recent headlines from Google News RSS for the given search query.
    /// </summary>
    Task<IReadOnlyList<NewsHeadline>> GetHeadlinesAsync(
        string searchQuery,
        int maxResults,
        CancellationToken cancellationToken = default);
}
