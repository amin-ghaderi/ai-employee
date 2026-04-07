using System.Text.Json;
using AiEmployee.Application.BotConfig;
using AiEmployee.Application.Dtos;
using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Options;
using AiEmployee.Application.Prompting;
using AiEmployee.Application.UseCases;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace AiEmployee.Application.Admin;

public sealed class PromptDebugService : IPromptDebugService
{
    private const string DefaultPreviewConversationId = "admin-preview";

    /// <summary>Minimal transcript when no DB conversation and no <paramref name="text"/> query (matches admin test line shape).</summary>
    private const string DefaultPreviewJudgeText = "Alice: Hello\nBob: Hi";

    private readonly IBotConfigurationRepository _botConfigurationRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IAiClient _aiClient;
    private readonly PromptBuilder _promptBuilder;
    private readonly JudgeUseCase _judgeUseCase;
    private readonly ILogger<PromptDebugService> _logger;

    public PromptDebugService(
        IBotConfigurationRepository botConfigurationRepository,
        IConversationRepository conversationRepository,
        IAiClient aiClient,
        PromptBuilder promptBuilder,
        JudgeUseCase judgeUseCase,
        ILogger<PromptDebugService> logger)
    {
        _botConfigurationRepository = botConfigurationRepository;
        _conversationRepository = conversationRepository;
        _aiClient = aiClient;
        _promptBuilder = promptBuilder;
        _judgeUseCase = judgeUseCase;
        _logger = logger;
    }

    public async Task<PromptDebugResponse> GetJudgeDebugAsync(
        Guid? botId,
        string? channel,
        string? externalId,
        string? conversationId,
        string? text,
        CancellationToken cancellationToken = default)
    {
        var response = new PromptDebugResponse
        {
            Channel = channel?.Trim() ?? string.Empty,
        };

        JudgeBotConfiguration? config = null;
        try
        {
            config = await ResolveConfigAsync(botId, channel, externalId).ConfigureAwait(false);
            if (config is null)
                return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Prompt debug: configuration resolution failed.");
            return response;
        }

        response.BotId = config.Bot.Id.ToString();
        response.PersonaId = config.Persona.Id.ToString();
        response.BehaviorId = config.Behavior.Id.ToString();
        response.Channel = string.IsNullOrWhiteSpace(response.Channel)
            ? config.Bot.Channel.ToString()
            : response.Channel;
        response.PromptSource = BehaviorPromptMapper.GetJudgePromptSource(config.Behavior);
        response.Schema = BehaviorPromptMapper.ParseSchema(config.Behavior.JudgeSchemaJson);

        var judgeTemplate = new BehaviorPromptMapper(NullLogger<BehaviorPromptMapper>.Instance)
            .BuildJudgePrompt(config.Persona, config.Behavior);
        response.HasInputToken = judgeTemplate.Contains(PromptTokens.Input, StringComparison.Ordinal);
        response.HasGoalToken = judgeTemplate.Contains(PromptTokens.Goal, StringComparison.Ordinal);
        response.HasExperienceToken = judgeTemplate.Contains(PromptTokens.Experience, StringComparison.Ordinal);

        var effectiveConversationId = string.IsNullOrWhiteSpace(conversationId)
            ? DefaultPreviewConversationId
            : conversationId.Trim();

        string textForJudge;
        try
        {
            var convo = await _conversationRepository.GetByIdAsync(effectiveConversationId).ConfigureAwait(false);
            if (convo is not null && convo.Messages.Count > 0)
            {
                textForJudge = _promptBuilder.BuildJudgeTranscript(convo, config.Behavior);
            }
            else
            {
                textForJudge = text?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(textForJudge)
                    && string.Equals(effectiveConversationId, DefaultPreviewConversationId, StringComparison.Ordinal))
                {
                    textForJudge = DefaultPreviewJudgeText;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Prompt debug: failed to resolve transcript; using query text only.");
            textForJudge = text?.Trim() ?? string.Empty;
        }

        JudgeExecutionContext execContext;
        try
        {
            execContext = await _judgeUseCase
                .BuildExecutionContextAsync(effectiveConversationId, textForJudge, config, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Prompt debug: BuildExecutionContextAsync failed; returning best-effort result.");
            execContext = new JudgeExecutionContext
            {
                PathType = "SIMPLE",
                Prompt = textForJudge,
                PromptHash = PromptHashing.ComputeSha256(textForJudge),
                Transcript = textForJudge,
            };
        }

        response.Prompt = execContext.Prompt ?? string.Empty;
        response.PathType = execContext.PathType;
        response.PromptHash = execContext.PromptHash;

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            JudgmentResultDto aiResult;
            if (string.Equals(execContext.PathType, "FULL", StringComparison.Ordinal))
            {
                aiResult = await _aiClient
                    .JudgeWithFullPromptAsync(
                        effectiveConversationId,
                        response.Prompt,
                        response.PromptHash)
                    .ConfigureAwait(false);
            }
            else
            {
                aiResult = await _aiClient
                    .JudgeAsync(effectiveConversationId, execContext.Prompt ?? string.Empty)
                    .ConfigureAwait(false);
            }

            sw.Stop();

            response.LatencyMs = sw.ElapsedMilliseconds;
            response.ParsedResult = aiResult;
            response.RawResponse = JsonSerializer.Serialize(aiResult);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Prompt debug: AI trace call failed; returning partial debug response.");
        }

        return response;
    }

    private async Task<JudgeBotConfiguration?> ResolveConfigAsync(
        Guid? botId,
        string? channel,
        string? externalId)
    {
        if (botId is Guid bid && bid != Guid.Empty)
            return await _botConfigurationRepository.GetByBotIdAsync(bid).ConfigureAwait(false);

        var normalizedChannel = channel?.Trim() ?? string.Empty;
        var normalizedExternalId = externalId?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(normalizedChannel) || !string.IsNullOrWhiteSpace(normalizedExternalId))
            return await _botConfigurationRepository.GetByIntegrationAsync(normalizedChannel, normalizedExternalId)
                .ConfigureAwait(false);

        return await _botConfigurationRepository.GetJudgeBotAsync().ConfigureAwait(false);
    }
}
