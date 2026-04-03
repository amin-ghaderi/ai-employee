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

        var normalizedWinner = NormalizeWinner(dto.Winner);

        return new JudgmentResult
        {
            Winner = normalizedWinner,
            Reason = dto.Reason
        };
    }

    private string NormalizeWinner(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "UNKNOWN";

        var text = raw.Trim().ToLower();

        if (text == "a" || text.Contains("option a") || text.Contains("first") || text.Contains("a "))
            return "A";

        if (text == "b" || text.Contains("option b") || text.Contains("second") || text.Contains("b "))
            return "B";

        return "UNKNOWN";
    }
}
