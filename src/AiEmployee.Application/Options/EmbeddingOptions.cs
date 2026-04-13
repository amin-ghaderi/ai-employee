namespace AiEmployee.Application.Options;

public sealed class EmbeddingOptions
{
    public const string SectionName = "Embedding";

    public string Provider { get; set; } = "Placeholder";

    public string Model { get; set; } = string.Empty;

    public int Dimensions { get; set; } = 1536;

    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}
