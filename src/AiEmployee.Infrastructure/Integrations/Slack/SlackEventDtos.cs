using System.Text.Json.Serialization;

namespace AiEmployee.Infrastructure.Integrations.Slack;

/// <summary>Subset of Slack Events API envelope JSON.</summary>
public sealed class SlackEventRequest
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("challenge")]
    public string? Challenge { get; set; }

    [JsonPropertyName("event")]
    public SlackEvent? Event { get; set; }
}

/// <summary>Subset of Slack <c>event</c> object for <c>message</c> events.</summary>
public sealed class SlackEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("subtype")]
    public string? Subtype { get; set; }

    [JsonPropertyName("user")]
    public string? User { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("channel")]
    public string? Channel { get; set; }

    [JsonPropertyName("ts")]
    public string? Ts { get; set; }

    [JsonPropertyName("thread_ts")]
    public string? ThreadTs { get; set; }

    [JsonPropertyName("bot_id")]
    public string? BotId { get; set; }
}
