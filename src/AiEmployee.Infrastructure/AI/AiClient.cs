using System.Net.Http.Json;
using AiEmployee.Domain.Interfaces;

namespace AiEmployee.Infrastructure.AI;

public class AiClient : IAiClient
{
    private readonly HttpClient _httpClient;

    public AiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> AskAsync(string prompt)
    {
        var response = await _httpClient.PostAsJsonAsync("http://localhost:8000/ai/judge", new JudgeRequest(prompt));
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<JudgeResponse>();
        return payload?.Result ?? string.Empty;
    }

    private sealed record JudgeRequest(string Text);
    private sealed record JudgeResponse(string Result);
}
