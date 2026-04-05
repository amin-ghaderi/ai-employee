using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using AiEmployee.Application;
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
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    "http://localhost:8000/ai/judge",
                    new { user_id = userId, text });
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
                throw new AiServiceException("AI judge request failed.", ex);
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
                throw new AiServiceException("AI judge request failed.", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new AiServiceException("AI judge request failed.", ex);
            }
        }

        throw new AiServiceException("AI judge request failed.");
    }

    public async Task<JudgmentResultDto> JudgeWithFullPromptAsync(string userId, string prompt)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    "http://localhost:8000/ai/judge/full",
                    new { user_id = userId, prompt });
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
                throw new AiServiceException("AI judge full prompt request failed.", ex);
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
                throw new AiServiceException("AI judge full prompt request failed.", ex);
            }
            catch (TaskCanceledException ex)
            {
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
                    "http://localhost:8000/ai/lead/classify",
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
}
