using System.Text.Json.Serialization;

namespace AiEmployee.Application.Dtos;

public sealed class JudgmentResultDto
{
    [JsonPropertyName("winner")]
    public string Winner { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}
