using AiEmployee.Application.Dtos;

namespace AiEmployee.Application.Interfaces;

public interface IAiClient
{
    Task<JudgmentResultDto> JudgeAsync(string userId, string text);

    Task<LeadClassificationDto> ClassifyLeadAsync(string prompt);
}
