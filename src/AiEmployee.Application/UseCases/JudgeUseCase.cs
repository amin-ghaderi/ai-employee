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

        var execContext = await BuildExecutionContextAsync(
                conversationId,
                text,
                config,
                cancellationToken)
            .ConfigureAwait(false);

        if (string.Equals(execContext.PathType, "FULL", StringComparison.Ordinal))
        {
            _logger.LogInformation(
                "Judge: PathType={PathType}, ConversationId={ConversationId}, BotId={BotId}, PersonaId={PersonaId}, PromptHash={PromptHash}, PromptVersion={PromptVersion}, UseFullJudgePrompt={UseFullJudgePrompt}, FinalPromptLength={FinalPromptLength}",
                execContext.PathType.ToLowerInvariant(),
                conversationId,
                botId,
                personaId,
                execContext.PromptHash,
                judgePromptCurrentVersion,
                useFull,
                execContext.Prompt.Length);

            dto = await _aiClient.JudgeWithFullPromptAsync(
                conversationId,
                execContext.Prompt,
                execContext.PromptHash);
        }
        else
        {
            _logger.LogInformation(
                "Judge: PathType={PathType}, ConversationId={ConversationId}, BotId={BotId}, PersonaId={PersonaId}, PromptHash={PromptHash}, PromptVersion={PromptVersion}, UseFullJudgePrompt={UseFullJudgePrompt}, PayloadChars={PayloadChars}",
                execContext.PathType.ToLowerInvariant(),
                conversationId,
                botId,
                personaId,
                execContext.PromptHash,
                judgePromptCurrentVersion,
                useFull,
                execContext.Prompt.Length);

            dto = await _aiClient.JudgeAsync(conversationId, execContext.Prompt);
        }

        var judgment = new Judgment(conversationId, userId, text, dto.Winner, dto.Reason);
        await _judgmentRepository.SaveAsync(judgment);

        return new JudgmentResult
        {
            Winner = dto.Winner,
            Reason = dto.Reason
        };
    }

    public async Task<JudgeExecutionContext> BuildExecutionContextAsync(
        string conversationId,
        string text,
        JudgeBotConfiguration config,
        CancellationToken cancellationToken = default)
    {
        var useFull = _aiOptions.Value.UseFullJudgePrompt;
        if (!useFull)
        {
            return BuildSimpleExecutionContext(text);
        }

        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        if (conversation is null || conversation.Messages.Count == 0)
        {
            if (!IsAdminTestConversationId(conversationId) || string.IsNullOrWhiteSpace(text))
            {
                return BuildSimpleExecutionContext(text);
            }

            conversation = BuildSyntheticAdminConversation(conversationId, text);
        }

        var built = _promptBuilder.BuildFullJudgePrompt(
            conversation,
            config.Behavior,
            config.Persona,
            config.WrapperTemplate);

        return new JudgeExecutionContext
        {
            PathType = "FULL",
            Prompt = built.Prompt,
            PromptHash = built.PromptHash,
            Transcript = text,
        };
    }

    private static bool IsAdminTestConversationId(string conversationId) =>
        !string.IsNullOrEmpty(conversationId)
        && (conversationId.StartsWith("admin-test", StringComparison.Ordinal)
            || string.Equals(conversationId, "admin-preview", StringComparison.Ordinal));

    /// <summary>
    /// Single-message conversation so <see cref="PromptBuilder.BuildJudgeTranscript"/> + <see cref="PromptBuilder.BuildFullJudgePrompt"/>
    /// can run when the store has no rows yet (admin test / debug only).
    /// </summary>
    private static Conversation BuildSyntheticAdminConversation(string conversationId, string text)
    {
        var conversation = new Conversation(conversationId);
        conversation.AddMessage(new Message(conversationId, "admin-synthetic", text.Trim(), firstName: "User"));
        return conversation;
    }

    private static JudgeExecutionContext BuildSimpleExecutionContext(string text) =>
        new()
        {
            PathType = "SIMPLE",
            Prompt = text,
            PromptHash = PromptHashing.ComputeSha256(text),
            Transcript = text,
        };
}
