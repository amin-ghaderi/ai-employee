namespace AiEmployee.Domain.BotConfiguration;

public sealed class LeadFlow
{
    public int? FollowUpIndex { get; private set; }
    public int? CaptureIndex { get; private set; }
    public IReadOnlyList<string> AnswerKeys { get; private set; } = Array.Empty<string>();

    private LeadFlow()
    {
    }

    public LeadFlow(int? followUpIndex, int? captureIndex, IReadOnlyList<string> answerKeys)
    {
        FollowUpIndex = followUpIndex;
        CaptureIndex = captureIndex;
        AnswerKeys = answerKeys ?? Array.Empty<string>();
    }
}
