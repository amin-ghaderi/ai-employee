namespace AiEmployee.Application.Admin;

public interface IJudgeExecutionService
{
    Task<JudgeExecutionResult> ExecuteWithDebugAsync(
        TestIntegrationJudgeRequest request,
        CancellationToken cancellationToken = default);
}
