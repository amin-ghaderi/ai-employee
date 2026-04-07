namespace AiEmployee.Application.Admin;

public sealed class TestLeadWithDebugRequest
{
    public Guid PersonaId { get; init; }
    public Guid BehaviorId { get; init; }

    public List<string> Answers { get; init; } = [];
    public List<string>? AnswerKeys { get; init; }

    public string? Channel { get; init; }
    public Guid? BotId { get; init; }
}
