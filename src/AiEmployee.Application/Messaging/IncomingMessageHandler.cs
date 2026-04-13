using AiEmployee.Application;
using AiEmployee.Application.Admin;
using AiEmployee.Application.BotConfig;
using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Prompting;
using AiEmployee.Application.Services;
using AiEmployee.Application.Telegram;
using AiEmployee.Application.UseCases;
using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AiEmployee.Application.Messaging;

public sealed class IncomingMessageHandler : IIncomingMessageHandler
{
    private readonly JudgeUseCase _judgeUseCase;
    private readonly IOutgoingMessageClient _outgoingClient;
    private readonly IConversationRepository _conversationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILeadRepository _leadRepository;
    private readonly AutomationService _automationService;
    private readonly LeadClassificationService _leadClassificationService;
    private readonly IBotResolver _botResolver;
    private readonly PromptBuilder _promptBuilder;
    private readonly UserTaggingService _userTaggingService;
    private readonly AssistantUseCase _assistantUseCase;
    private readonly IFlowTracker _flowTracker;
    private readonly RealFlowTestContext _testContext;
    private readonly IActiveTelegramBotContext _activeTelegramBotContext;
    private readonly ITelegramUpdateDeduplicator _telegramUpdateDeduplicator;
    private readonly IMessageEmbeddingPublisher _messageEmbeddingPublisher;
    private readonly ILogger<IncomingMessageHandler> _logger;

    public IncomingMessageHandler(
        JudgeUseCase judgeUseCase,
        IOutgoingMessageClient outgoingClient,
        IConversationRepository conversationRepository,
        IUserRepository userRepository,
        ILeadRepository leadRepository,
        AutomationService automationService,
        LeadClassificationService leadClassificationService,
        IBotResolver botResolver,
        PromptBuilder promptBuilder,
        UserTaggingService userTaggingService,
        AssistantUseCase assistantUseCase,
        IFlowTracker flowTracker,
        RealFlowTestContext testContext,
        IActiveTelegramBotContext activeTelegramBotContext,
        ITelegramUpdateDeduplicator telegramUpdateDeduplicator,
        IMessageEmbeddingPublisher messageEmbeddingPublisher,
        ILogger<IncomingMessageHandler> logger)
    {
        _judgeUseCase = judgeUseCase;
        _outgoingClient = outgoingClient;
        _conversationRepository = conversationRepository;
        _userRepository = userRepository;
        _leadRepository = leadRepository;
        _automationService = automationService;
        _leadClassificationService = leadClassificationService;
        _botResolver = botResolver;
        _promptBuilder = promptBuilder;
        _userTaggingService = userTaggingService;
        _assistantUseCase = assistantUseCase;
        _flowTracker = flowTracker;
        _testContext = testContext;
        _activeTelegramBotContext = activeTelegramBotContext;
        _telegramUpdateDeduplicator = telegramUpdateDeduplicator;
        _messageEmbeddingPublisher = messageEmbeddingPublisher;
        _logger = logger;
    }

    public async Task HandleAsync(IncomingMessage message, CancellationToken cancellationToken = default)
    {
        var conversationId = message.ExternalChatId;
        var messageUserId = message.ExternalUserId;
        var text = message.Text;
        var username = Meta(message.Metadata, IncomingMessageMetadataKeys.Username);
        var firstName = Meta(message.Metadata, IncomingMessageMetadataKeys.FirstName);
        var lastName = Meta(message.Metadata, IncomingMessageMetadataKeys.LastName);

        LanguageProfile? languageProfile = null;

        try
        {
            var integrationId = Meta(message.Metadata, IncomingMessageMetadataKeys.IntegrationExternalId);
            _logger.LogInformation(
                "IncomingMessageHandler | channel={Channel} userId={UserId} chatId={ChatId} textLength={TextLength} hasIntegrationExternalId={HasIntegration}",
                message.Channel,
                messageUserId,
                conversationId,
                text?.Length ?? 0,
                !string.IsNullOrWhiteSpace(integrationId));

            var dedupScope = Meta(message.Metadata, IncomingMessageMetadataKeys.TelegramBotScopeKey);
            var dedupUpdateIdText = Meta(message.Metadata, IncomingMessageMetadataKeys.TelegramUpdateId);
            if (BotIntegrationChannelNames.IsTelegramChannel(message.Channel)
                && !string.IsNullOrWhiteSpace(dedupScope)
                && long.TryParse(dedupUpdateIdText, out var dedupUpdateId))
            {
                if (!await _telegramUpdateDeduplicator.TryRegisterFirstDeliveryAsync(dedupScope, dedupUpdateId).ConfigureAwait(false))
                {
                    _logger.LogInformation(
                        "Skipping duplicate Telegram update | update_id={UpdateId} scope={Scope}",
                        dedupUpdateId,
                        dedupScope);
                    return;
                }
            }

            var user = await _userRepository.GetByIdAsync(messageUserId)
                ?? new User(messageUserId);
            user.UpdateProfile(username, firstName, lastName);
            user.RegisterMessage();

            var judgeBotConfig = await _botResolver.ResolveAsync(message);
            _activeTelegramBotContext.Token = string.IsNullOrWhiteSpace(judgeBotConfig.TelegramBotToken)
                ? null
                : judgeBotConfig.TelegramBotToken.Trim();

            var behavior = judgeBotConfig.Behavior;
            languageProfile = judgeBotConfig.LanguageProfile;

            _userTaggingService.Apply(user, behavior.EngagementRules);
            await _userRepository.SaveAsync(user);

            _logger.LogInformation(
                "User updated: {UserId}, Messages={Count}",
                user.Id,
                user.MessagesCount);

            var tokens = (text ?? string.Empty).Replace("\n", " ")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var commandToken = tokens
                .Select(t => t.Split('@')[0])
                .FirstOrDefault(t => t.StartsWith('/'));
            var isJudgeCommand = commandToken is not null
                && string.Equals(
                    commandToken,
                    behavior.JudgeCommandPrefix,
                    StringComparison.OrdinalIgnoreCase);
            async Task RunAutomationAsync()
            {
                if (_testContext.IsActive && _testContext.DisableAutomation)
                    return;

                var actions = _automationService.Evaluate(user, behavior.AutomationRules);
                foreach (var action in actions)
                {
                    switch (action)
                    {
                        case AutomationActionKind.SendReactivationMessage:
                            await _outgoingClient.SendMessageAsync(
                                message.Channel,
                                message.ExternalChatId,
                                languageProfile.ReactivationMessage);
                            break;
                        case AutomationActionKind.NotifyAdminHighEngagement:
                            _logger.LogInformation("High engagement user detected: {UserId}", user.Id);
                            break;
                    }
                }

                await _userRepository.SaveAsync(user);
            }

            _logger.LogInformation("Incoming text: {Text}", text);
            _logger.LogInformation("Parsed command: {Command}", commandToken);
            _logger.LogInformation(
                "Judge routing | isJudgeCommand={IsJudge}, EnableJudge={EnableJudge}, JudgeCommandPrefix={Prefix}, BehaviorId={BehaviorId}",
                isJudgeCommand,
                behavior.EnableJudge,
                behavior.JudgeCommandPrefix,
                behavior.Id);

            var shouldRunJudge = isJudgeCommand && behavior.EnableJudge;

            if (shouldRunJudge)
            {
                _flowTracker.Set("judge");
                var existing = await _conversationRepository.GetByIdAsync(conversationId);
                if (existing is null || existing.Messages.Count == 0)
                {
                    await _outgoingClient.SendMessageAsync(
                        message.Channel,
                        message.ExternalChatId,
                        languageProfile.JudgeNoConversationMessage);
                    await RunAutomationAsync();
                    return;
                }

                var transcript = _promptBuilder.BuildJudgeTranscript(existing, behavior);

                if (string.IsNullOrEmpty(transcript))
                {
                    await _outgoingClient.SendMessageAsync(
                        message.Channel,
                        message.ExternalChatId,
                        languageProfile.JudgeNotEnoughContextMessage);
                    await RunAutomationAsync();
                    return;
                }

                var conversationResult = await _judgeUseCase.Execute(
                    conversationId,
                    messageUserId,
                    transcript,
                    judgeBotConfig);

                _logger.LogInformation(
                    "AI Judgment Result | Winner: {Winner} | Reason: {Reason}",
                    conversationResult.Winner,
                    conversationResult.Reason);

                var judgeReply = languageProfile.JudgeResultTemplate
                    .Replace("{Reason}", conversationResult.Reason, StringComparison.Ordinal)
                    .Replace("{Winner}", conversationResult.Winner, StringComparison.Ordinal);
                await _outgoingClient.SendMessageAsync(
                    message.Channel,
                    message.ExternalChatId,
                    judgeReply);

                await RunAutomationAsync();
                return;
            }

            if (behavior.OnboardingFirstMessageOnly
                && user.MessagesCount == 1)
            {
                _flowTracker.Set("onboarding");
                var onboardingMessage = new Message(conversationId, messageUserId, text, username, firstName, lastName);
                await _conversationRepository.AppendUserMessageAsync(conversationId, onboardingMessage).ConfigureAwait(false);
                await TryEnqueueMessageEmbeddingAsync(onboardingMessage.Id).ConfigureAwait(false);

                await _outgoingClient.SendMessageAsync(
                    message.Channel,
                    message.ExternalChatId,
                    languageProfile.OnboardingGoalQuestion);
                return;
            }

            var msg = new Message(conversationId, messageUserId, text, username, firstName, lastName);
            await _conversationRepository.AppendUserMessageAsync(conversationId, msg).ConfigureAwait(false);
            await TryEnqueueMessageEmbeddingAsync(msg.Id).ConfigureAwait(false);

            _logger.LogInformation("Message saved to conversation {ConversationId}", conversationId);

            var conversation = await _conversationRepository.GetByIdAsync(conversationId)
                ?? throw new InvalidOperationException($"Conversation '{conversationId}' not found after append.");

            var userMessages = conversation.Messages
                .Where(m => m.UserId == messageUserId)
                .OrderBy(m => m.CreatedAt)
                .ToList();

            var leadFlow = behavior.LeadFlow;
            var answerKeys = leadFlow.AnswerKeys;
            var n = answerKeys.Count;

            var existingLeads = await _leadRepository.GetByUserIdAsync(user.Id);

            var leadFollowUpWouldRun = behavior.EnableLead
                && leadFlow.FollowUpIndex is int followUpIndex
                && userMessages.Count == followUpIndex;

            var leadCaptureWouldRun = behavior.EnableLead
                && leadFlow.CaptureIndex is int captureIndex
                && userMessages.Count >= captureIndex
                && !existingLeads.Any()
                && n > 0
                && userMessages.Count >= n;

            var shouldRunLead = leadFollowUpWouldRun || leadCaptureWouldRun;

            var chatEligible =
                behavior.EnableChat
                && !isJudgeCommand
                && !(behavior.OnboardingFirstMessageOnly && user.MessagesCount == 1 && !isJudgeCommand);

            var shouldRunChat = chatEligible && !shouldRunLead;

            if (shouldRunLead)
            {
                _flowTracker.Set("lead");
                if (leadFlow.FollowUpIndex is int followUpIdx && userMessages.Count == followUpIdx)
                {
                    await _outgoingClient.SendMessageAsync(
                        message.Channel,
                        message.ExternalChatId,
                        languageProfile.ExperienceFollowUpQuestion);
                }

                if (leadFlow.CaptureIndex is int captureIdx && userMessages.Count >= captureIdx)
                {
                    if (!existingLeads.Any())
                    {
                        if (userMessages.Count >= n && n > 0)
                        {
                            var start = userMessages.Count - n;
                            var lead = new Lead(user.Id);
                            for (var i = 0; i < n; i++)
                                lead.Answers[answerKeys[i]] = userMessages[start + i].Text;

                            var (userType, intent, potential) =
                                await _leadClassificationService.ClassifyAsync(
                                    judgeBotConfig.Persona,
                                    behavior,
                                    lead.Answers,
                                    behavior.LeadFlow.AnswerKeys);
                            lead.UserType = userType;
                            lead.Intent = intent;
                            lead.Potential = potential;

                            await _leadRepository.SaveAsync(lead);

                            if (string.Equals(
                                    potential,
                                    behavior.HotLeadPotentialValue,
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                user.Tags.Add(behavior.HotLeadTag);
                                await _userRepository.SaveAsync(user);
                            }

                            await _outgoingClient.SendMessageAsync(
                                message.Channel,
                                message.ExternalChatId,
                                languageProfile.LeadThanksMessage);
                        }
                    }
                }
            }
            else if (shouldRunChat)
            {
                _flowTracker.Set("chat");
                var response = await _assistantUseCase.Execute(
                    conversationId,
                    messageUserId,
                    text,
                    judgeBotConfig,
                    cancellationToken).ConfigureAwait(false);
                await _outgoingClient.SendMessageAsync(
                    message.Channel,
                    message.ExternalChatId,
                    response);

                try
                {
                    var assistantMessage = Message.CreateAssistant(
                        conversationId,
                        judgeBotConfig.Bot.Id.ToString("D"),
                        response);
                    await _conversationRepository
                        .AppendAssistantMessageAsync(conversationId, assistantMessage)
                        .ConfigureAwait(false);
                    await TryEnqueueMessageEmbeddingAsync(assistantMessage.Id).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to persist assistant message for conversation {ConversationId}",
                        conversationId);
                }
            }
            else
            {
                _logger.LogWarning(
                    "No outbound reply for this message | EnableChat={EnableChat} EnableLead={EnableLead} shouldRunLead={RunLead} shouldRunChat={RunChat} isJudgeCommand={IsJudge} onboardingFirstOnly={OnboardingFirst} userMessagesCount={UserMsgCount} behaviorId={BehaviorId}",
                    behavior.EnableChat,
                    behavior.EnableLead,
                    shouldRunLead,
                    shouldRunChat,
                    isJudgeCommand,
                    behavior.OnboardingFirstMessageOnly,
                    user.MessagesCount,
                    behavior.Id);
            }

            await RunAutomationAsync();
        }
        catch (AiServiceException ex)
        {
            _logger.LogError(
                ex,
                "AI service request failed (inner: {Inner})",
                ex.InnerException?.Message ?? "(none)");

            if (_testContext.IsActive)
            {
                _testContext.PipelineError = ex.InnerException is { } inner
                    ? $"{ex.Message} [{inner.GetType().Name}: {inner.Message}]"
                    : ex.Message;
            }

            try
            {
                await _outgoingClient.SendMessageAsync(
                    message.Channel,
                    message.ExternalChatId,
                    languageProfile is not null
                        ? languageProfile.GenericErrorMessage
                        : "⚠️ Something went wrong. Please try again.");
            }
            catch (Exception notifyEx)
            {
                _logger.LogError(notifyEx, "Failed to send error notification via outgoing client");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Incoming message handling failed");
            throw;
        }
        finally
        {
            _activeTelegramBotContext.Token = null;
        }
    }

    private async Task TryEnqueueMessageEmbeddingAsync(Guid messageId)
    {
        try
        {
            await _messageEmbeddingPublisher.EnqueueAsync(messageId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enqueue message embedding | messageId={MessageId}", messageId);
        }
    }

    private static string? Meta(IReadOnlyDictionary<string, string>? metadata, string key) =>
        metadata is not null && metadata.TryGetValue(key, out var v) ? v : null;
}
