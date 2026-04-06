namespace AiEmployee.Application.Dtos.Behaviors;

public sealed class LeadFlowDto
{
    public int? FollowUpIndex { get; set; }
    public int? CaptureIndex { get; set; }
    public IReadOnlyList<string> AnswerKeys { get; set; } = Array.Empty<string>();
}
