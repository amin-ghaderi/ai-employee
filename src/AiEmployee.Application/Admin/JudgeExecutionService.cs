using System.Diagnostics;
using System.Text.Json;
using AiEmployee.Application.BotConfig;
using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Messaging;
using AiEmployee.Application.Options;
using AiEmployee.Application.Prompting;
using AiEmployee.Application.UseCases;
using AiEmployee.Domain.BotConfiguration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AiEmployee.Application.Admin;

public sealed class JudgeExecutionService : IJudgeExecutionService
{
    private const string TestConversationId = "admin-test-judge-with-debug";
    private const string TestUserId = "admin-test";

    private readonly IConversationRepository _conversationRepository;
    private readonly IBotResolver _botResolver;
    private readonly PromptBuilder _promptBuilder;
    private readonly IAiClient _aiClient;
    private readonly JudgeUseCase _judgeUseCase;
    private readonly ILogger<JudgeExecutionService> _logger;

    public JudgeExecutionService(
        IConversationRepository conversationRepository,
        IBotResolver botResolver,
        PromptBuilder promptBuilder,
        IAiClient aiClient,
        JudgeUseCase judgeUseCase,
        ILogger<JudgeExecutionService> logger)
    {
        _conversationRepository = conversationRepository;
        _botResolver = botResolver;
        _promptBuilder = promptBuilder;
        _aiClient = aiClient;
        _judgeUseCase = judgeUseCase;
        _logger = logger;
    }

    public async Task<JudgeExecutionResult> ExecuteWithDebugAsync(
        TestIntegrationJudgeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var debug = new PromptDebugResponse();
        try
        {
            var messages = BuildAdminTestMessages(request.Text);
            await _conversationRepository
                .ReplaceMessagesAsync(TestConversationId, messages, cancellationToken)
                .ConfigureAwait(false);

            var channel = request.Channel?.Trim() ?? string.Empty;
            var externalId = request.ExternalId?.Trim() ?? string.Empty;
            var metadata = string.IsNullOrWhiteSpace(externalId)
                ? null
                : new Dictionary<string, string>
                {
                    [IncomingMessageMetadataKeys.IntegrationExternalId] = externalId,
                };

            var resolveMessage = new IncomingMessage(
                string.IsNullOrWhiteSpace(channel) ? BotIntegrationChannelNames.Telegram : channel,
                TestUserId,
                TestConversationId,
                "admin-test-resolve",
                metadata);

            var config = await _botResolver.ResolveAsync(resolveMessage).ConfigureAwait(false);
            debug.BotId = config.Bot.Id.ToString();
            debug.PersonaId = config.Persona.Id.ToString();
            debug.BehaviorId = config.Behavior.Id.ToString();
            debug.Channel = resolveMessage.Channel;
            debug.PromptSource = BehaviorPromptMapper.GetJudgePromptSource(config.Behavior);
            debug.Schema = BehaviorPromptMapper.ParseSchema(config.Behavior.JudgeSchemaJson);

            var judgeTemplate = new BehaviorPromptMapper(NullLogger<BehaviorPromptMapper>.Instance)
                .BuildJudgePrompt(config.Persona, config.Behavior);
            debug.HasInputToken = judgeTemplate.Contains(PromptTokens.Input, StringComparison.Ordinal);
            debug.HasGoalToken = judgeTemplate.Contains(PromptTokens.Goal, StringComparison.Ordinal);
            debug.HasExperienceToken = judgeTemplate.Contains(PromptTokens.Experience, StringComparison.Ordinal);

            var conversation = await _conversationRepository.GetByIdAsync(TestConversationId).ConfigureAwait(false);
            string transcript;
            if (conversation is not null && conversation.Messages.Count > 0)
            {
                transcript = _promptBuilder.BuildJudgeTranscript(conversation, config.Behavior);
            }
            else
            {
                transcript = request.Text?.Trim() ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(transcript))
            {
                debug.PathType = "SIMPLE";
                return new JudgeExecutionResult { Debug = debug };
            }

            var execContext = await _judgeUseCase
                .BuildExecutionContextAsync(TestConversationId, transcript, config, cancellationToken)
                .ConfigureAwait(false);
            var pathType = execContext.PathType;
            var promptForAi = execContext.Prompt;
            var promptHash = execContext.PromptHash;

            var sw = Stopwatch.StartNew();
            var aiResult = pathType == "FULL"
                ? await _aiClient.JudgeWithFullPromptAsync(TestConversationId, promptForAi, promptHash).ConfigureAwait(false)
                : await _aiClient.JudgeAsync(TestConversationId, promptForAi).ConfigureAwait(false);
            sw.Stop();

            debug.PathType = pathType;
            debug.Prompt = promptForAi;
            debug.PromptHash = promptHash;
            debug.LatencyMs = sw.ElapsedMilliseconds;
            debug.ParsedResult = aiResult;
            debug.RawResponse = JsonSerializer.Serialize(aiResult);

            return new JudgeExecutionResult
            {
                Winner = aiResult.Winner,
                Reason = aiResult.Reason,
                Debug = debug,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Judge execution with debug failed; returning partial debug payload.");
            return new JudgeExecutionResult { Debug = debug };
        }
    }

    private static List<AiEmployee.Domain.Entities.Message> BuildAdminTestMessages(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var lines = text.Trim().Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        var participantIndex = 0;
        var participantIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var messages = new List<AiEmployee.Domain.Entities.Message>(lines.Length);
        foreach (var line in lines)
        {
            var (firstName, messageText) = ParseTranscriptLine(line);

            var participantKey = firstName ?? $"__anon_{participantIndex}";
            if (!participantIds.TryGetValue(participantKey, out var userId))
            {
                userId = $"admin-sim-{participantIndex}";
                participantIds[participantKey] = userId;
                participantIndex++;
            }

            messages.Add(new AiEmployee.Domain.Entities.Message(TestConversationId, userId, messageText, firstName: firstName));
        }

        return messages;
    }

    private static (string? Name, string Text) ParseTranscriptLine(string line)
    {
        var colonIndex = line.IndexOf(':');
        if (colonIndex <= 0)
            return (null, line);

        var name = line[..colonIndex].Trim();
        if (name.Length == 0 || name.Length > 50)
            return (null, line);

        var text = line[(colonIndex + 1)..].Trim();
        if (text.Length == 0)
            return (null, line);

        return (name, text);
    }

}
