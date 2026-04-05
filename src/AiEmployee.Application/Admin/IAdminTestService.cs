namespace AiEmployee.Application.Admin;

public interface IAdminTestService
{
    Task<TestJudgeResponse> JudgeAsync(TestJudgeRequest request, CancellationToken cancellationToken = default);
}
