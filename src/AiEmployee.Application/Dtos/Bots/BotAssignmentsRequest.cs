namespace AiEmployee.Application.Dtos.Bots;

public sealed class BotAssignmentsRequest
{
    public Guid PersonaId { get; set; }
    public Guid BehaviorId { get; set; }
    public Guid LanguageProfileId { get; set; }
}
