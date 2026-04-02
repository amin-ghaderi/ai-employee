using AiEmployee.Domain.Interfaces;

namespace AiEmployee.Domain.Services;

public class JudgeService
{
    private readonly IAiClient _aiClient;

    public JudgeService(IAiClient aiClient)
    {
        _aiClient = aiClient;
    }

    public async Task<string> Process(string text)
    {
        var prompt = $"Analyze this conversation and decide who is right:\n{text}";
        return await _aiClient.AskAsync(prompt);
    }
}
