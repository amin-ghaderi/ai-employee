using System.Text.Json.Serialization;

namespace AiEmployee.Api.Models;

public class TelegramUpdate
{
    [JsonPropertyName("message")]
    public TelegramMessage? Message { get; set; }
}

public class TelegramMessage
{
    [JsonPropertyName("chat")]
    public TelegramChat? Chat { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class TelegramChat
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
}
