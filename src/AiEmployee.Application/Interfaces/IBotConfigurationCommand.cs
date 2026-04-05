namespace AiEmployee.Application.Interfaces;

public interface IBotConfigurationCommand
{
    Task UpdatePromptsAsync(Guid botId, string judgePrompt, string leadPrompt, CancellationToken cancellationToken = default);
}
