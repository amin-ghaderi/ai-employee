using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiEmployee.Application.Messaging;
using AiEmployee.Domain.BotConfiguration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiEmployee.Infrastructure.Integrations.Slack;

public sealed class SlackMessageSender : IChannelMessageSender
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;
    private readonly SlackSettings _settings;
    private readonly ILogger<SlackMessageSender> _logger;

    public SlackMessageSender(
        HttpClient httpClient,
        IOptions<SlackSettings> settings,
        ILogger<SlackMessageSender> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public string Channel => IntegrationProviders.Slack;

    public async Task SendAsync(string externalChatId, string text)
    {
        var token = _settings.BotToken?.Trim();
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Slack send skipped: Slack:BotToken is not configured.");
            return;
        }

        var (channel, threadTs) = ParseSlackDestination(externalChatId);
        if (string.IsNullOrEmpty(channel))
        {
            _logger.LogWarning("Slack send skipped: channel id missing (payload length={Length}).", externalChatId?.Length ?? 0);
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat.postMessage");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");

        var payload = new ChatPostMessageRequest(channel, text, threadTs);
        request.Content = JsonContent.Create(payload, options: SerializerOptions);

        using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Slack chat.postMessage HTTP error | status={Status} body={Body}",
                (int)response.StatusCode,
                body.Length > 512 ? body[..512] + "…" : body);
            return;
        }

        try
        {
            var envelope = JsonSerializer.Deserialize<SlackApiOkResponse>(body, SerializerOptions);
            if (envelope is { Ok: false })
            {
                _logger.LogWarning(
                    "Slack chat.postMessage returned ok=false | error={Error}",
                    envelope.Error ?? body);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Slack chat.postMessage response was not valid JSON.");
        }
    }

    private static (string Channel, string? ThreadTs) ParseSlackDestination(string? externalChatId)
    {
        var raw = externalChatId?.Trim();
        if (string.IsNullOrEmpty(raw))
            return (string.Empty, null);

        var pipe = raw.IndexOf('|', StringComparison.Ordinal);
        if (pipe <= 0 || pipe >= raw.Length - 1)
            return (raw, null);

        return (raw[..pipe], raw[(pipe + 1)..]);
    }

    private sealed record ChatPostMessageRequest(
        [property: JsonPropertyName("channel")] string Channel,
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("thread_ts")] string? ThreadTs);

    private sealed record SlackApiOkResponse(
        [property: JsonPropertyName("ok")] bool Ok,
        [property: JsonPropertyName("error")] string? Error);
}
