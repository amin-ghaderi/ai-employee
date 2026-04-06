using AiEmployee.Application.BotConfig;
using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Prompting;

namespace AiEmployee.Application.UseCases;

public sealed class AssistantUseCase
{
    private readonly IAiClient _aiClient;
    private readonly PromptComposer _promptComposer;

    public AssistantUseCase(IAiClient aiClient, PromptComposer promptComposer)
    {
        _aiClient = aiClient;
        _promptComposer = promptComposer;
    }

    public async Task<string> Execute(
        string userId,
        string userInput,
        JudgeBotConfiguration config)
    {
        var prompt = _promptComposer.BuildChatPrompt(config.Persona, userInput);
        return await _aiClient.ChatAsync(userId, prompt);
    }
}
