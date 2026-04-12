using AiEmployee.Application.BotConfig;
using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Prompting;
using Microsoft.Extensions.Logging;

namespace AiEmployee.Application.UseCases;

public sealed class AssistantUseCase
{
    private readonly IAiClient _aiClient;
    private readonly PromptComposer _promptComposer;
    private readonly ILogger<AssistantUseCase> _logger;

    public AssistantUseCase(
        IAiClient aiClient,
        PromptComposer promptComposer,
        ILogger<AssistantUseCase> logger)
    {
        _aiClient = aiClient;
        _promptComposer = promptComposer;
        _logger = logger;
    }

    public async Task<string> Execute(
        string userId,
        string userInput,
        JudgeBotConfiguration config)
    {
        var prompt = _promptComposer.BuildChatPrompt(config.Persona, userInput);
        _logger.LogInformation(
            "AssistantUseCase | personaId={PersonaId} userId={UserId} promptChars={PromptChars}",
            config.Persona.Id,
            userId,
            prompt.Length);
        return await _aiClient.ChatAsync(userId, prompt);
    }
}
