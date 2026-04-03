using AiEmployee.Application.Interfaces;
using AiEmployee.Domain.Entities;

namespace AiEmployee.Application.UseCases;

public class JudgeUseCase
{
    private readonly IAiClient _aiClient;

    public JudgeUseCase(IAiClient aiClient)
    {
        _aiClient = aiClient;
    }

    public async Task<JudgmentResult> Execute(string userId, string text)
    {
        var dto = await _aiClient.JudgeAsync(userId, text);

        return new JudgmentResult
        {
            Winner = dto.Winner,
            Reason = dto.Reason
        };
    }
}
