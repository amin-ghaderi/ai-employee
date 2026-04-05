using AiEmployee.Application.Dtos;

namespace AiEmployee.Application.Interfaces;

public interface IAiClient
{
    Task<JudgmentResultDto> JudgeAsync(string userId, string text);

    Task<JudgmentResultDto> JudgeWithFullPromptAsync(string userId, string prompt);

    Task<LeadClassificationDto> ClassifyLeadAsync(string prompt);
}
