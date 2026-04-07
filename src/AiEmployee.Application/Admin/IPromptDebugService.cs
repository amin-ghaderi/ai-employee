namespace AiEmployee.Application.Admin;

public interface IPromptDebugService
{
    Task<PromptDebugResponse> GetJudgeDebugAsync(
        Guid? botId,
        string? channel,
        string? externalId,
        string? conversationId,
        string? text,
        CancellationToken cancellationToken = default);
}
