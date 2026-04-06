namespace AiEmployee.Application.Dtos.Bots;

public sealed class UpdateBotRequest
{
    public string? Name { get; set; }
    public bool? IsEnabled { get; set; }
}
