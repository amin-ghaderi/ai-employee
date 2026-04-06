namespace AiEmployee.Application.Admin;

public interface IAdminTestService
{
    Task<TestJudgeResponse> JudgeAsync(TestJudgeRequest request, CancellationToken cancellationToken = default);

    Task<TestJudgeResponse> JudgeByIntegrationAsync(
        TestIntegrationJudgeRequest request,
        CancellationToken cancellationToken = default);
}
