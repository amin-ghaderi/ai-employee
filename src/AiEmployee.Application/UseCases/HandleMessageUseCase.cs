using AiEmployee.Domain.Services;

namespace AiEmployee.Application.UseCases;

public class HandleMessageUseCase
{
    private readonly JudgeService _judgeService;

    public HandleMessageUseCase(JudgeService judgeService)
    {
        _judgeService = judgeService;
    }

    public async Task<string> Execute(string userId, string text)
    {
        var payload = $"User:{userId}; Message:{text}";
        return await _judgeService.Process(payload);
    }
}
