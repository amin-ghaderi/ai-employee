using AiEmployee.Application.BotConfig;
using AiEmployee.Application.Dtos;
using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Options;
using AiEmployee.Application.Prompting;
using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiEmployee.Application.UseCases;

public class JudgeUseCase
{
    private readonly IAiClient _aiClient;
    private readonly IJudgmentRepository _judgmentRepository;
    private readonly IOptions<AiOptions> _aiOptions;
    private readonly IConversationRepository _conversationRepository;
    private readonly IPromptVersionReadRepository _promptVersionRead;
    private readonly PromptBuilder _promptBuilder;
    private readonly ILogger<JudgeUseCase> _logger;

    public JudgeUseCase(
        IAiClient aiClient,
        IJudgmentRepository judgmentRepository,
        IOptions<AiOptions> aiOptions,
        IConversationRepository conversationRepository,
        IPromptVersionReadRepository promptVersionRead,
        PromptBuilder promptBuilder,
        ILogger<JudgeUseCase> logger)
    {
        _aiClient = aiClient;
        _judgmentRepository = judgmentRepository;
        _aiOptions = aiOptions;
        _conversationRepository = conversationRepository;
        _promptVersionRead = promptVersionRead;
        _promptBuilder = promptBuilder;
        _logger = logger;
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
        JudgeBotConfiguration config,
        CancellationToken cancellationToken = default)
    {
        JudgmentResultDto dto;

        var botId = config.Bot.Id;
        var personaId = config.Persona.Id;
        var maxArchivedJudgeVersion = await _promptVersionRead
            .GetMaxVersionAsync(personaId, PromptType.Judge, cancellationToken)
            .ConfigureAwait(false);
        var judgePromptCurrentVersion = maxArchivedJudgeVersion + 1;

        var useFull = _aiOptions.Value.UseFullJudgePrompt;
        _logger.LogInformation(
            "Judge: UseFullJudgePrompt={UseFullJudgePrompt}, ConversationId={ConversationId}, BotId={BotId}, PersonaId={PersonaId}, PromptVersion={PromptVersion}",
            useFull,
            conversationId,
            botId,
            personaId,
            judgePromptCurrentVersion);

        if (useFull)
        {
            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            if (conversation is null)
            {
                var simpleHash = PromptHashing.ComputeSha256(text);
                _logger.LogInformation(
                    "Judge: PathType={PathType}, ConversationId={ConversationId}, BotId={BotId}, PersonaId={PersonaId}, PromptHash={PromptHash}, PromptVersion={PromptVersion}, UseFullJudgePrompt={UseFullJudgePrompt}, PayloadChars={PayloadChars}",
                    "simple",
                    conversationId,
                    botId,
                    personaId,
                    simpleHash,
                    judgePromptCurrentVersion,
                    useFull,
                    text.Length);

                dto = await _aiClient.JudgeAsync(conversationId, text);
            }
            else
            {
                var judgeHasInput = config.Persona.Prompts.Judge.Contains(
                    PromptTokens.Input,
                    StringComparison.Ordinal);
                _logger.LogInformation(
                    "Judge full prompt path: ConversationId={ConversationId}, BotId={BotId}, PersonaId={PersonaId}, PromptVersion={PromptVersion}, JudgeHasInputPlaceholder={HasInput}",
                    conversationId,
                    botId,
                    personaId,
                    judgePromptCurrentVersion,
                    judgeHasInput);

                var built = _promptBuilder.BuildFullJudgePrompt(
                    conversation,
                    config.Behavior,
                    config.Persona,
                    config.WrapperTemplate);

                _logger.LogInformation(
                    "Judge: PathType={PathType}, ConversationId={ConversationId}, BotId={BotId}, PersonaId={PersonaId}, PromptHash={PromptHash}, PromptVersion={PromptVersion}, UseFullJudgePrompt={UseFullJudgePrompt}, FinalPromptLength={FinalPromptLength}",
                    "full",
                    conversationId,
                    botId,
                    personaId,
                    built.PromptHash,
                    judgePromptCurrentVersion,
                    useFull,
                    built.Prompt.Length);

                dto = await _aiClient.JudgeWithFullPromptAsync(
                    conversationId,
                    built.Prompt,
                    built.PromptHash);
            }
        }
        else
        {
            var simpleHash = PromptHashing.ComputeSha256(text);
            _logger.LogInformation(
                "Judge: PathType={PathType}, ConversationId={ConversationId}, BotId={BotId}, PersonaId={PersonaId}, PromptHash={PromptHash}, PromptVersion={PromptVersion}, UseFullJudgePrompt={UseFullJudgePrompt}, PayloadChars={PayloadChars}",
                "simple",
                conversationId,
                botId,
                personaId,
                simpleHash,
                judgePromptCurrentVersion,
                useFull,
                text.Length);

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
