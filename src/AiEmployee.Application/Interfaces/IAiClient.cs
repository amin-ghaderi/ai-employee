using AiEmployee.Application.Dtos;

namespace AiEmployee.Application.Interfaces;

public interface IAiClient
{
    Task<JudgmentResultDto> JudgeAsync(string prompt);
}
