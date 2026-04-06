using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using AiEmployee.Application;
using AiEmployee.Application.Dtos;
using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiEmployee.Infrastructure.AI;

public class AiClient : IAiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AiClient> _logger;
    private readonly string _baseUrl;

    public AiClient(HttpClient httpClient, IOptions<AiOptions> options, ILogger<AiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        var baseUrl = options.Value.BaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = "http://localhost:8000";
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<JudgmentResultDto> JudgeAsync(string userId, string text)
    {
        var endpoint = $"{_baseUrl}/ai/judge";
        var payloadChars = text.Length;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            _logger.LogInformation(
                "AI JudgeAsync request: Endpoint={Endpoint}, UserId={UserId}, PayloadChars={PayloadChars}, Attempt={Attempt}",
                endpoint,
                userId,
                payloadChars,
                attempt + 1);

            var sw = Stopwatch.StartNew();
            try
            {
                using var response = await _httpClient.PostAsJsonAsync(
                    endpoint,
                    new { user_id = userId, text });
                sw.Stop();

                var statusCode = (int)response.StatusCode;
                var outcome = response.IsSuccessStatusCode ? "success" : "failure";
                _logger.LogInformation(
                    "AI JudgeAsync response: Outcome={Outcome}, StatusCode={StatusCode}, LatencyMs={LatencyMs}, Attempt={Attempt}",
                    outcome,
                    statusCode,
                    sw.ElapsedMilliseconds,
                    attempt + 1);

                response.EnsureSuccessStatusCode();

                var dto = await response.Content.ReadFromJsonAsync<JudgmentResultDto>();
                if (dto is null)
                    throw new AiServiceException("AI returned empty judge response.");

                return dto;
            }
            catch (AiServiceException)
            {
                throw;
            }
            catch (JsonException ex)
            {
                sw.Stop();
                _logger.LogWarning(
                    ex,
                    "AI JudgeAsync: Outcome=failure, LatencyMs={LatencyMs}, Attempt={Attempt}, Reason=json",
                    sw.ElapsedMilliseconds,
                    attempt + 1);
                throw new AiServiceException("AI judge request failed.", ex);
            }
            catch (HttpRequestException ex) when (attempt < 2)
            {
                sw.Stop();
                _logger.LogWarning(
                    ex,
                    "AI JudgeAsync: Outcome=failure, LatencyMs={LatencyMs}, Attempt={Attempt}, Reason=http",
                    sw.ElapsedMilliseconds,
                    attempt + 1);
                await Task.Delay(500 * (attempt + 1));
            }
            catch (TaskCanceledException ex) when (attempt < 2)
            {
                sw.Stop();
                _logger.LogWarning(
                    ex,
                    "AI JudgeAsync: Outcome=failure, LatencyMs={LatencyMs}, Attempt={Attempt}, Reason=timeout",
                    sw.ElapsedMilliseconds,
                    attempt + 1);
                await Task.Delay(500 * (attempt + 1));
            }
            catch (HttpRequestException ex)
            {
                sw.Stop();
                _logger.LogWarning(
                    ex,
                    "AI JudgeAsync: Outcome=failure, LatencyMs={LatencyMs}, Attempt={Attempt}, Reason=http",
                    sw.ElapsedMilliseconds,
                    attempt + 1);
                throw new AiServiceException("AI judge request failed.", ex);
            }
            catch (TaskCanceledException ex)
            {
                sw.Stop();
                _logger.LogWarning(
                    ex,
                    "AI JudgeAsync: Outcome=failure, LatencyMs={LatencyMs}, Attempt={Attempt}, Reason=timeout",
                    sw.ElapsedMilliseconds,
                    attempt + 1);
                throw new AiServiceException("AI judge request failed.", ex);
            }
        }

        throw new AiServiceException("AI judge request failed.");
    }

    public async Task<JudgmentResultDto> JudgeWithFullPromptAsync(string userId, string prompt, string? promptHash = null)
    {
        var endpoint = $"{_baseUrl}/ai/judge/full";
        var payloadChars = prompt.Length;
        var promptHashForLog = promptHash ?? string.Empty;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            _logger.LogInformation(
                "AI JudgeWithFullPromptAsync request: Endpoint={Endpoint}, UserId={UserId}, PayloadChars={PayloadChars}, PromptHash={PromptHash}, Attempt={Attempt}",
                endpoint,
                userId,
                payloadChars,
                promptHashForLog,
                attempt + 1);

            var sw = Stopwatch.StartNew();
            try
            {
                using var response = await _httpClient.PostAsJsonAsync(
                    endpoint,
                    new { user_id = userId, prompt });
                sw.Stop();

                var statusCode = (int)response.StatusCode;
                var outcome = response.IsSuccessStatusCode ? "success" : "failure";
                _logger.LogInformation(
                    "AI JudgeWithFullPromptAsync response: Outcome={Outcome}, StatusCode={StatusCode}, LatencyMs={LatencyMs}, PromptHash={PromptHash}, Attempt={Attempt}",
                    outcome,
                    statusCode,
                    sw.ElapsedMilliseconds,
                    promptHashForLog,
                    attempt + 1);

                response.EnsureSuccessStatusCode();

                var dto = await response.Content.ReadFromJsonAsync<JudgmentResultDto>();
                if (dto is null)
                    throw new AiServiceException("AI returned empty judge response.");

                return dto;
            }
            catch (AiServiceException)
            {
                throw;
            }
            catch (JsonException ex)
            {
                sw.Stop();
                _logger.LogWarning(
                    ex,
                    "AI JudgeWithFullPromptAsync: Outcome=failure, LatencyMs={LatencyMs}, PromptHash={PromptHash}, Attempt={Attempt}, Reason=json",
                    sw.ElapsedMilliseconds,
                    promptHashForLog,
                    attempt + 1);
                throw new AiServiceException("AI judge full prompt request failed.", ex);
            }
            catch (HttpRequestException ex) when (attempt < 2)
            {
                sw.Stop();
                _logger.LogWarning(
                    ex,
                    "AI JudgeWithFullPromptAsync: Outcome=failure, LatencyMs={LatencyMs}, PromptHash={PromptHash}, Attempt={Attempt}, Reason=http",
                    sw.ElapsedMilliseconds,
                    promptHashForLog,
                    attempt + 1);
                await Task.Delay(500 * (attempt + 1));
            }
            catch (TaskCanceledException ex) when (attempt < 2)
            {
                sw.Stop();
                _logger.LogWarning(
                    ex,
                    "AI JudgeWithFullPromptAsync: Outcome=failure, LatencyMs={LatencyMs}, PromptHash={PromptHash}, Attempt={Attempt}, Reason=timeout",
                    sw.ElapsedMilliseconds,
                    promptHashForLog,
                    attempt + 1);
                await Task.Delay(500 * (attempt + 1));
            }
            catch (HttpRequestException ex)
            {
                sw.Stop();
                _logger.LogWarning(
                    ex,
                    "AI JudgeWithFullPromptAsync: Outcome=failure, LatencyMs={LatencyMs}, PromptHash={PromptHash}, Attempt={Attempt}, Reason=http",
                    sw.ElapsedMilliseconds,
                    promptHashForLog,
                    attempt + 1);
                throw new AiServiceException("AI judge full prompt request failed.", ex);
            }
            catch (TaskCanceledException ex)
            {
                sw.Stop();
                _logger.LogWarning(
                    ex,
                    "AI JudgeWithFullPromptAsync: Outcome=failure, LatencyMs={LatencyMs}, PromptHash={PromptHash}, Attempt={Attempt}, Reason=timeout",
                    sw.ElapsedMilliseconds,
                    promptHashForLog,
                    attempt + 1);
                throw new AiServiceException("AI judge full prompt request failed.", ex);
            }
        }

        throw new AiServiceException("AI judge full prompt request failed.");
    }

    public async Task<LeadClassificationDto> ClassifyLeadAsync(string prompt)
    {
        _logger.LogInformation("Calling AI lead classification...");

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    $"{_baseUrl}/ai/lead/classify",
                    new { prompt });

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<LeadClassificationDto>();

                if (result is null)
                    throw new AiServiceException("AI returned null classification response.");

                return result;
            }
            catch (AiServiceException)
            {
                throw;
            }
            catch (JsonException ex)
            {
                throw new AiServiceException("AI lead classification request failed.", ex);
            }
            catch (HttpRequestException) when (attempt < 2)
            {
                await Task.Delay(500 * (attempt + 1));
            }
            catch (TaskCanceledException) when (attempt < 2)
            {
                await Task.Delay(500 * (attempt + 1));
            }
            catch (HttpRequestException ex)
            {
                throw new AiServiceException("AI lead classification request failed.", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new AiServiceException("AI lead classification request failed.", ex);
            }
        }

        throw new AiServiceException("AI lead classification request failed.");
    }

    public async Task<string> ChatAsync(string userId, string prompt)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    $"{_baseUrl}/ai/chat",
                    new { user_id = userId, prompt });
                response.EnsureSuccessStatusCode();

                var raw = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(raw))
                    throw new AiServiceException("AI returned empty chat response.");

                return ParseChatResponse(raw.Trim());
            }
            catch (AiServiceException)
            {
                throw;
            }
            catch (JsonException ex)
            {
                throw new AiServiceException("AI chat request failed.", ex);
            }
            catch (HttpRequestException) when (attempt < 2)
            {
                await Task.Delay(500 * (attempt + 1));
            }
            catch (TaskCanceledException) when (attempt < 2)
            {
                await Task.Delay(500 * (attempt + 1));
            }
            catch (HttpRequestException ex)
            {
                throw new AiServiceException("AI chat request failed.", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new AiServiceException("AI chat request failed.", ex);
            }
        }

        throw new AiServiceException("AI chat request failed.");
    }

    private static string ParseChatResponse(string raw)
    {
        if (raw.Length > 0 && raw[0] == '{')
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("response", out var el) &&
                    el.ValueKind == JsonValueKind.String)
                    return el.GetString() ?? string.Empty;
            }
            catch (JsonException)
            {
                // Not valid JSON; return body as plain text.
            }
        }

        return raw;
    }
}
