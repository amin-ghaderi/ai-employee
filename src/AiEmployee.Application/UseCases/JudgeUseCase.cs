using AiEmployee.Application.BotConfig;
using AiEmployee.Application.Dtos;
using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Options;
using AiEmployee.Application.Prompting;
using AiEmployee.Domain.Entities;
using Microsoft.Extensions.Options;

namespace AiEmployee.Application.UseCases;

public class JudgeUseCase
{
    private readonly IAiClient _aiClient;
    private readonly IJudgmentRepository _judgmentRepository;
    private readonly IOptions<AiOptions> _aiOptions;
    private readonly IConversationRepository _conversationRepository;
    private readonly PromptBuilder _promptBuilder;

    public JudgeUseCase(
        IAiClient aiClient,
        IJudgmentRepository judgmentRepository,
        IOptions<AiOptions> aiOptions,
        IConversationRepository conversationRepository,
        PromptBuilder promptBuilder)
    {
        _aiClient = aiClient;
        _judgmentRepository = judgmentRepository;
        _aiOptions = aiOptions;
        _conversationRepository = conversationRepository;
        _promptBuilder = promptBuilder;
    }

    /// <summary>
    /// Runs the judge flow. <paramref name="conversationId"/> is passed to the AI client as the session key.
    /// <paramref name="text"/> is the transcript from the controller; when <see cref="AiOptions.UseFullJudgePrompt"/> is true, the conversation is reloaded to assemble the full prompt in .NET.
    /// <paramref name="config"/> supplies persona, behavior, and transcript wrapper template for the full-prompt path.
    /// </summary>
    public async Task<JudgmentResult> Execute(
        string conversationId,
        string userId,
        string text,
        JudgeBotConfiguration config)
    {
        JudgmentResultDto dto;

        if (_aiOptions.Value.UseFullJudgePrompt)
        {
            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            if (conversation is null)
            {
                dto = await _aiClient.JudgeAsync(conversationId, text);
            }
            else
            {
                var fullPrompt = _promptBuilder.BuildFullJudgePrompt(
                    conversation,
                    config.Behavior,
                    config.Persona,
                    config.WrapperTemplate);
                dto = await _aiClient.JudgeWithFullPromptAsync(conversationId, fullPrompt);
            }
        }
        else
        {
            dto = await _aiClient.JudgeAsync(conversationId, text);
        }

        var judgment = new Judgment(conversationId, userId, text, dto.Winner, dto.Reason);
        await _judgmentRepository.SaveAsync(judgment);

        return new JudgmentResult
        {
            Winner = dto.Winner,
            Reason = dto.Reason
        };
    }
}
