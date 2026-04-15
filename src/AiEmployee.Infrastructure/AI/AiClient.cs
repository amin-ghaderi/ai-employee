using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using AiEmployee.Application.Admin;
using AiEmployee.Application;
using AiEmployee.Application.Dtos;
using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Options;
using AiEmployee.Application.Prompting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiEmployee.Infrastructure.AI;

public class AiClient : IAiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AiClient> _logger;
    private readonly IOptions<AiOptions> _options;
    private readonly IChatOutputSchemaValidator _chatOutputSchemaValidator;
    private readonly string _baseUrl;

    public AiClient(
        HttpClient httpClient,
        IOptions<AiOptions> options,
        ILogger<AiClient> logger,
        IChatOutputSchemaValidator chatOutputSchemaValidator)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options;
        _chatOutputSchemaValidator = chatOutputSchemaValidator;
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
                var rawResponse = await response.Content.ReadAsStringAsync();
                AiDebugContext.SetLastRawResponse(rawResponse);
                var dto = JsonSerializer.Deserialize<JudgmentResultDto>(rawResponse);
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
                var rawResponse = await response.Content.ReadAsStringAsync();
                AiDebugContext.SetLastRawResponse(rawResponse);
                var dto = JsonSerializer.Deserialize<JudgmentResultDto>(rawResponse);
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
                var rawResponse = await response.Content.ReadAsStringAsync();
                AiDebugContext.SetLastRawResponse(rawResponse);
                var result = JsonSerializer.Deserialize<LeadClassificationDto>(rawResponse);

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

    public async Task<string> ChatAsync(
        string userId,
        string prompt,
        ChatCompletionRequestContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var opts = _options.Value;
        var enforceSchema = opts.EnforceChatOutputSchema && HasNonTrivialChatSchema(context?.ChatOutputSchemaJson);
        using var activity = AiClientTelemetry.ActivitySource.StartActivity("ai.chat");
        activity?.SetTag("ai.persona.id", context?.PersonaId?.ToString() ?? string.Empty);
        activity?.SetTag("ai.conversation.id", context?.ConversationId ?? string.Empty);
        activity?.SetTag("ai.chat.schema.enforced", enforceSchema.ToString());

        using var logScope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["AiOperation"] = "chat",
            ["PersonaId"] = context?.PersonaId,
            ["ConversationId"] = context?.ConversationId,
            ["ChatSchemaEnforced"] = enforceSchema,
        });

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var response = await _httpClient.PostAsJsonAsync(
                    $"{_baseUrl}/ai/chat",
                    new { user_id = userId, prompt },
                    cancellationToken);
                var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "AI chat HTTP {StatusCode} (attempt {Attempt}). Response body preview: {BodyPreview}",
                        (int)response.StatusCode,
                        attempt + 1,
                        TruncateForLog(raw, 2000));
                    response.EnsureSuccessStatusCode();
                }

                if (string.IsNullOrWhiteSpace(raw))
                    throw new AiServiceException("AI returned empty chat response.");

                var inner = ParseChatResponse(raw.Trim());
                var validated = ProcessChatStructuredOutput(inner, context, enforceSchema, activity);
                sw.Stop();
                AiClientTelemetry.ChatCompletionDurationSeconds.Record(
                    sw.Elapsed.TotalSeconds,
                    new TagList { { "schema_enforced", enforceSchema } });

                _logger.LogInformation(
                    "AI chat completed: Outcome=success, LatencyMs={LatencyMs}, Attempt={Attempt}, SchemaEnforced={SchemaEnforced}, ResponseChars={ResponseChars}",
                    sw.ElapsedMilliseconds,
                    attempt + 1,
                    enforceSchema,
                    validated.Length);

                activity?.SetStatus(ActivityStatusCode.Ok);
                return validated;
            }
            catch (AiServiceException ex)
            {
                sw.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogWarning(
                    ex,
                    "AI chat failed after {LatencyMs}ms (attempt {Attempt}): {Message}",
                    sw.ElapsedMilliseconds,
                    attempt + 1,
                    ex.Message);
                throw;
            }
            catch (JsonException ex)
            {
                sw.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw new AiServiceException("AI chat request failed.", ex);
            }
            catch (HttpRequestException ex) when (attempt < 2)
            {
                sw.Stop();
                _logger.LogWarning(
                    ex,
                    "AI chat HTTP retry after {LatencyMs}ms (attempt {Attempt})",
                    sw.ElapsedMilliseconds,
                    attempt + 1);
                await Task.Delay(500 * (attempt + 1), cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException ex) when (attempt < 2 && !cancellationToken.IsCancellationRequested)
            {
                sw.Stop();
                _logger.LogWarning(
                    ex,
                    "AI chat timeout retry after {LatencyMs}ms (attempt {Attempt})",
                    sw.ElapsedMilliseconds,
                    attempt + 1);
                await Task.Delay(500 * (attempt + 1), cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                sw.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw new AiServiceException("AI chat request failed.", ex);
            }
            catch (TaskCanceledException ex)
            {
                sw.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw new AiServiceException("AI chat request failed.", ex);
            }
        }

        activity?.SetStatus(ActivityStatusCode.Error);
        throw new AiServiceException("AI chat request failed.");
    }

    private string ProcessChatStructuredOutput(
        string inner,
        ChatCompletionRequestContext? context,
        bool enforceSchema,
        Activity? activity)
    {
        if (!enforceSchema || context is null || string.IsNullOrWhiteSpace(context.ChatOutputSchemaJson))
            return inner;

        if (!ChatAssistantJsonPayloadExtractor.TryExtractJsonObject(inner, out var jsonPayload))
        {
            AiClientTelemetry.ChatSchemaValidationFailures.Add(1);
            activity?.SetTag("ai.chat.schema.result", "no_json_object");
            _logger.LogWarning(
                "Chat output schema enforced but model returned no JSON object. PersonaId={PersonaId}, Preview={Preview}",
                context.PersonaId,
                TruncateForLog(inner, 400));
            throw new AiServiceException(
                "Assistant reply must be a JSON object matching the configured chat output schema.");
        }

        var schemaJson = context.ChatOutputSchemaJson!;
        var error = _chatOutputSchemaValidator.TryValidate(jsonPayload, schemaJson);
        if (error is not null)
        {
            AiClientTelemetry.ChatSchemaValidationFailures.Add(1);
            activity?.SetTag("ai.chat.schema.result", "invalid");
            _logger.LogWarning(
                "Chat JSON schema validation failed. PersonaId={PersonaId}, Error={Error}, Preview={Preview}",
                context.PersonaId,
                error,
                TruncateForLog(jsonPayload, 600));
            throw new AiServiceException(
                "Assistant reply did not satisfy the configured chat output JSON schema: " + error);
        }

        using var doc = JsonDocument.Parse(jsonPayload);
        var visible = ChatStructuredResponseFormatter.ToUserVisibleText(doc.RootElement);
        activity?.SetTag("ai.chat.schema.result", "valid");
        _logger.LogDebug("Chat JSON schema validation succeeded for PersonaId={PersonaId}", context.PersonaId);
        return visible;
    }

    private static bool HasNonTrivialChatSchema(string? schemaJson)
    {
        if (string.IsNullOrWhiteSpace(schemaJson))
            return false;
        var t = schemaJson.Trim();
        return !string.Equals(t, "{}", StringComparison.Ordinal)
            && !string.Equals(t, "null", StringComparison.OrdinalIgnoreCase);
    }

    private static string TruncateForLog(string? text, int maxChars)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxChars)
            return text ?? string.Empty;
        return text[..maxChars] + "…[truncated]";
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
