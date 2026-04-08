using AiEmployee.Application.BotConfig;
using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Messaging;
using AiEmployee.Application.Prompting;
using AiEmployee.Application.UseCases;
using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Domain.Entities;

namespace AiEmployee.Application.Admin;

/// <summary>
/// Simulates the Telegram <c>/judge</c> path in <see cref="IncomingMessageHandler"/> (resolve bot →
/// <see cref="PromptBuilder.BuildJudgeTranscript"/> → <see cref="JudgeUseCase.Execute"/>).
/// With <c>Ai:UseFullJudgePrompt</c> true and this conversation stored, <see cref="JudgeUseCase"/> uses
/// <c>JudgeWithFullPromptAsync</c> (HTTP <c>/ai/judge/full</c> on the AI service).
/// </summary>
public sealed class AdminTestService : IAdminTestService
{
    public const string AdminTestConversationId = "admin-test-judge";

    private const string TestUserId = "admin-test";

    private readonly IConversationRepository _conversationRepository;
    private readonly IBotResolver _botResolver;
    private readonly PromptBuilder _promptBuilder;
    private readonly JudgeUseCase _judgeUseCase;

    public AdminTestService(
        IConversationRepository conversationRepository,
        IBotResolver botResolver,
        PromptBuilder promptBuilder,
        JudgeUseCase judgeUseCase)
    {
        _conversationRepository = conversationRepository;
        _botResolver = botResolver;
        _promptBuilder = promptBuilder;
        _judgeUseCase = judgeUseCase;
    }

    public Task<TestJudgeResponse> JudgeAsync(TestJudgeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var messages = BuildAdminTestMessages(request.Text);

        // Same resolver path as Telegram when integration id is absent: falls back to default judge bot.
        var resolveMessage = new IncomingMessage(
            BotIntegrationChannelNames.Telegram,
            TestUserId,
            AdminTestConversationId,
            "admin-test-resolve",
            Metadata: null);

        return RunJudgeAsync(messages, resolveMessage, cancellationToken);
    }

    public Task<TestJudgeResponse> JudgeByIntegrationAsync(
        TestIntegrationJudgeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var messages = BuildAdminTestMessages(request.Text);

        var channel = request.Channel?.Trim() ?? string.Empty;
        var externalId = request.ExternalId?.Trim() ?? string.Empty;
        var metadata = new Dictionary<string, string>
        {
            [IncomingMessageMetadataKeys.IntegrationExternalId] = externalId,
        };

        var resolveMessage = new IncomingMessage(
            channel,
            TestUserId,
            AdminTestConversationId,
            "admin-test-resolve",
            metadata);

        return RunJudgeAsync(messages, resolveMessage, cancellationToken);
    }

    private static List<Message> BuildAdminTestMessages(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be null or empty.", nameof(text));

        var lines = text.Trim().Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
            throw new ArgumentException("Text cannot be null or empty.", nameof(text));

        var participantIndex = 0;
        var participantIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var messages = new List<Message>(lines.Length);
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

            messages.Add(new Message(AdminTestConversationId, userId, messageText, firstName: firstName));
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

    private async Task<TestJudgeResponse> RunJudgeAsync(
        IReadOnlyList<Message> messages,
        IncomingMessage resolveMessage,
        CancellationToken cancellationToken)
    {
        await _conversationRepository
            .ReplaceMessagesAsync(AdminTestConversationId, messages, cancellationToken)
            .ConfigureAwait(false);

        var config = await _botResolver.ResolveAsync(resolveMessage).ConfigureAwait(false);

        var conversation = await _conversationRepository.GetByIdAsync(AdminTestConversationId).ConfigureAwait(false);
        if (conversation is null || conversation.Messages.Count == 0)
            throw new InvalidOperationException("Admin test conversation could not be loaded after replace.");

        var transcript = _promptBuilder.BuildJudgeTranscript(conversation, config.Behavior);
        if (string.IsNullOrEmpty(transcript))
            throw new InvalidOperationException("No transcript could be built for the admin test conversation.");

        var judgment = await _judgeUseCase.Execute(
                AdminTestConversationId,
                TestUserId,
                transcript,
                config)
            .ConfigureAwait(false);

        return new TestJudgeResponse
        {
            Winner = judgment.Winner,
            Reason = judgment.Reason,
        };
    }
}
