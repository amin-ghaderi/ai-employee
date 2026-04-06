namespace AiEmployee.Application.Dtos.Bots;

public sealed class CreateBotRequest
{
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}
