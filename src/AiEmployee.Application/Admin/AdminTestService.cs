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

    public async Task<TestJudgeResponse> JudgeAsync(TestJudgeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Text))
            throw new ArgumentException("Text cannot be null or empty.", nameof(request));

        var lines = request.Text.Trim().Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
            throw new ArgumentException("Text cannot be null or empty.", nameof(request));

        var messages = new List<Message>(lines.Length);
        for (var i = 0; i < lines.Length; i++)
        {
            var userId = (i % 2 == 0) ? "admin-sim-a" : "admin-sim-b";
            var firstName = (i % 2 == 0) ? "Alice" : "Bob";
            messages.Add(new Message(AdminTestConversationId, userId, lines[i], firstName: firstName));
        }

        await _conversationRepository
            .ReplaceMessagesAsync(AdminTestConversationId, messages, cancellationToken)
            .ConfigureAwait(false);

        // Same resolver path as Telegram when integration id is absent: falls back to default judge bot.
        var resolveMessage = new IncomingMessage(
            BotIntegrationChannelNames.Telegram,
            TestUserId,
            AdminTestConversationId,
            "admin-test-resolve",
            Metadata: null);

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
            Reason = judgment.Reason
        };
    }
}
