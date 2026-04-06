using AiEmployee.Application;
using AiEmployee.Application.BotConfig;
using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Prompting;
using AiEmployee.Application.Services;
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
        _logger = logger;
    }

    public async Task HandleAsync(IncomingMessage message)
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
            Console.WriteLine("WEBHOOK HIT");
            Console.WriteLine($"TEXT RECEIVED: {text}");

            var user = await _userRepository.GetByIdAsync(messageUserId)
                ?? new User(messageUserId);
            user.UpdateProfile(username, firstName, lastName);
            user.RegisterMessage();

            var judgeBotConfig = await _botResolver.ResolveAsync(message);
            var behavior = judgeBotConfig.Behavior;
            languageProfile = judgeBotConfig.LanguageProfile;

            _userTaggingService.Apply(user, behavior.EngagementRules);
            await _userRepository.SaveAsync(user);

            _logger.LogInformation(
                "User updated: {UserId}, Messages={Count}",
                user.Id,
                user.MessagesCount);

            var commandToken = text.Split(' ')[0];
            commandToken = commandToken.Split('@')[0];
            var isJudgeCommand = string.Equals(
                commandToken,
                behavior.JudgeCommandPrefix,
                StringComparison.OrdinalIgnoreCase);
            if (isJudgeCommand)
                Console.WriteLine("JUDGE COMMAND DETECTED");

            async Task RunAutomationAsync()
            {
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

            if (behavior.OnboardingFirstMessageOnly
                && user.MessagesCount == 1
                && !isJudgeCommand)
            {
                var onboardingConversation = await _conversationRepository.GetByIdAsync(conversationId)
                    ?? new Conversation(conversationId);
                var onboardingMessage = new Message(conversationId, messageUserId, text, username, firstName, lastName);
                onboardingConversation.AddMessage(onboardingMessage);
                await _conversationRepository.SaveAsync(onboardingConversation);

                await _outgoingClient.SendMessageAsync(
                    message.Channel,
                    message.ExternalChatId,
                    languageProfile.OnboardingGoalQuestion);
                return;
            }

            var command = commandToken;

            _logger.LogInformation("Incoming text: {Text}", text);
            _logger.LogInformation("Parsed command: {Command}", command);

            var shouldRunJudge = isJudgeCommand && behavior.EnableJudge;

            if (shouldRunJudge)
            {
                Console.WriteLine("ENTERED JUDGE BLOCK");
                var existing = await _conversationRepository.GetByIdAsync(conversationId);
                if (existing is null || existing.Messages.Count == 0)
                {
                    Console.WriteLine("SENDING OUTGOING RESPONSE");
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

                Console.WriteLine("CALLING JUDGE USE CASE");
                var conversationResult = await _judgeUseCase.Execute(
                    conversationId,
                    messageUserId,
                    transcript,
                    judgeBotConfig);

                _logger.LogInformation(
                    "AI Judgment Result | Winner: {Winner} | Reason: {Reason}",
                    conversationResult.Winner,
                    conversationResult.Reason);

                Console.WriteLine("SENDING OUTGOING RESPONSE");
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

            var conversation = await _conversationRepository.GetByIdAsync(conversationId)
                ?? new Conversation(conversationId);
            var msg = new Message(conversationId, messageUserId, text, username, firstName, lastName);
            conversation.AddMessage(msg);
            await _conversationRepository.SaveAsync(conversation);

            _logger.LogInformation("Message saved to conversation {ConversationId}", conversationId);

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
                var response = await _assistantUseCase.Execute(
                    messageUserId,
                    text,
                    judgeBotConfig);
                await _outgoingClient.SendMessageAsync(
                    message.Channel,
                    message.ExternalChatId,
                    response);
            }

            await RunAutomationAsync();
        }
        catch (AiServiceException ex)
        {
            _logger.LogError(ex, "AI service request failed");

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
    }

    private static string? Meta(IReadOnlyDictionary<string, string>? metadata, string key) =>
        metadata is not null && metadata.TryGetValue(key, out var v) ? v : null;
}
