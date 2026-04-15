using AiEmployee.Application.Dtos;
using AiEmployee.Application.Prompting;

namespace AiEmployee.Application.Interfaces;

public interface IAiClient
{
    Task<JudgmentResultDto> JudgeAsync(string userId, string text);

    Task<JudgmentResultDto> JudgeWithFullPromptAsync(string userId, string prompt, string? promptHash = null);

    Task<LeadClassificationDto> ClassifyLeadAsync(string prompt);

    Task<string> ChatAsync(
        string userId,
        string prompt,
        ChatCompletionRequestContext? context = null,
        CancellationToken cancellationToken = default);
}
