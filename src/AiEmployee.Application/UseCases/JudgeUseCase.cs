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
        var prompt = $"Analyze this conversation and decide who is right:\nUser:{userId}; Message:{text}";
        var dto = await _aiClient.JudgeAsync(prompt);

        return new JudgmentResult
        {
            Winner = dto.Winner,
            Reason = dto.Reason
        };
    }
}
