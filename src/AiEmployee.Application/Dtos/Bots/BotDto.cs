namespace AiEmployee.Application.Dtos.Bots;

public sealed class BotDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid PersonaId { get; set; }
    public Guid BehaviorId { get; set; }
    public Guid LanguageProfileId { get; set; }
    public bool IsEnabled { get; set; }
}
