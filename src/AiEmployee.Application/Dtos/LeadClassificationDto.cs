using System.Text.Json.Serialization;

namespace AiEmployee.Application.Dtos;

public sealed class LeadClassificationDto
{
    [JsonPropertyName("user_type")]
    public string UserType { get; set; } = string.Empty;

    [JsonPropertyName("intent")]
    public string Intent { get; set; } = string.Empty;

    [JsonPropertyName("potential")]
    public string Potential { get; set; } = string.Empty;
}
