namespace AiEmployee.Application.Prompting;

public static class PromptTokens
{
    public const string Input = "{{input}}";
    public const string Transcript = "{{TRANSCRIPT}}";
    public const string Goal = "{{goal}}";
    public const string Experience = "{{experience}}";

    public const string JudgeWrapperMissingTranscriptMessage =
        "Wrapper template missing '{{TRANSCRIPT}}' placeholder. Transcript not injected.";

    /// <summary>Judge transcript wrapper templates must include <see cref="Transcript"/> or the conversation block is never injected.</summary>
    public static void ThrowIfJudgeWrapperMissingTranscriptPlaceholder(string? template)
    {
        if (string.IsNullOrEmpty(template) || !template.Contains(Transcript, StringComparison.Ordinal))
            throw new InvalidOperationException(JudgeWrapperMissingTranscriptMessage);
    }
}
