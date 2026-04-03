using System.Net.Http.Json;
using AiEmployee.Application.Dtos;
using AiEmployee.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AiEmployee.Infrastructure.AI;

public class AiClient : IAiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AiClient> _logger;

    public AiClient(HttpClient httpClient, ILogger<AiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<JudgmentResultDto> JudgeAsync(string userId, string text)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "http://localhost:8000/ai/judge",
            new { user_id = userId, text });
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<JudgmentResultDto>();
        return dto ?? new JudgmentResultDto();
    }

    public async Task<LeadClassificationDto> ClassifyLeadAsync(string prompt)
    {
        _logger.LogInformation("Calling AI lead classification...");

        var response = await _httpClient.PostAsJsonAsync(
            "http://localhost:8000/ai/lead/classify",
            new { prompt });

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LeadClassificationDto>();

        if (result is null)
            throw new InvalidOperationException("AI returned null.");

        return result;
    }
}
