namespace AiEmployee.Application.Options;

public sealed class RagOptions
{
    public const string SectionName = "Rag";

    public bool Enabled { get; set; }

    public int MaxVectorResults { get; set; } = 10;

    /// <summary>Minimum cosine similarity in [0,1] for search hits (1 = identical).</summary>
    public double MinSimilarity { get; set; } = 0.5;

    public int MaxSlidingWindowMessages { get; set; } = 40;

    /// <summary>Maximum characters per message when rendering sliding-window history (and per retrieved snippet line).</summary>
    public int MaxCharsPerMessage { get; set; } = 2000;
}
