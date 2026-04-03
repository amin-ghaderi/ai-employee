using System.Net.Http.Json;
using AiEmployee.Application.Dtos;
using AiEmployee.Application.Interfaces;

namespace AiEmployee.Infrastructure.AI;

public class AiClient : IAiClient
{
    private readonly HttpClient _httpClient;

    public AiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
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
}
