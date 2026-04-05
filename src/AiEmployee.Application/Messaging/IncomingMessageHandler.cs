using AiEmployee.Application;
using AiEmployee.Application.BotConfig;
using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Prompting;
using AiEmployee.Application.Services;
using AiEmployee.Application.UseCases;
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

        try
        {
            Console.WriteLine("WEBHOOK HIT");
            Console.WriteLine($"TEXT RECEIVED: {text}");

            var user = await _userRepository.GetByIdAsync(messageUserId)
                ?? new User(messageUserId);
            user.UpdateProfile(username, firstName, lastName);
            user.RegisterMessage();
            await _userRepository.SaveAsync(user);

            _logger.LogInformation(
                "User updated: {UserId}, Messages={Count}",
                user.Id,
                user.MessagesCount);

            var judgeBotConfig = await _botResolver.ResolveAsync(message);
            var behavior = judgeBotConfig.Behavior;
            var languageProfile = judgeBotConfig.LanguageProfile;

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
                var actions = _automationService.Evaluate(user);
                foreach (var action in actions)
                {
                    if (action == "send_reactivation_message")
                    {
                        await _outgoingClient.SendMessageAsync(
                            message.Channel,
                            message.ExternalChatId,
                            "We miss you! Come back to the conversation 🙂");
                    }

                    if (action == "notify_admin_high_engagement")
                    {
                        _logger.LogInformation("High engagement user detected: {UserId}", user.Id);
                    }
                }

                await _userRepository.SaveAsync(user);
            }

            if (user.MessagesCount == 1 && !isJudgeCommand)
            {
                var onboardingConversation = await _conversationRepository.GetByIdAsync(conversationId)
                    ?? new Conversation(conversationId);
                var onboardingMessage = new Message(conversationId, messageUserId, text, username, firstName, lastName);
                onboardingConversation.AddMessage(onboardingMessage);
                await _conversationRepository.SaveAsync(onboardingConversation);

                await _outgoingClient.SendMessageAsync(
                    message.Channel,
                    message.ExternalChatId,
                    "Hey! Quick question — what is your goal?");
                return;
            }

            var command = commandToken;

            _logger.LogInformation("Incoming text: {Text}", text);
            _logger.LogInformation("Parsed command: {Command}", command);

            if (isJudgeCommand)
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

            if (userMessages.Count == 2)
            {
                await _outgoingClient.SendMessageAsync(
                    message.Channel,
                    message.ExternalChatId,
                    "Got it 👍 What's your experience level?");
            }

            if (userMessages.Count >= 3)
            {
                var existingLeads = await _leadRepository.GetByUserIdAsync(user.Id);
                if (!existingLeads.Any())
                {
                    var goal = userMessages[^2].Text;
                    var experience = userMessages[^1].Text;

                    var lead = new Lead(user.Id);
                    lead.Answers["goal"] = goal;
                    lead.Answers["experience"] = experience;

                    var (userType, intent, potential) =
                        await _leadClassificationService.ClassifyAsync(lead.Answers);
                    lead.UserType = userType;
                    lead.Intent = intent;
                    lead.Potential = potential;

                    await _leadRepository.SaveAsync(lead);

                    if (string.Equals(potential, "high", StringComparison.OrdinalIgnoreCase))
                    {
                        user.Tags.Add("hot_lead");
                        await _userRepository.SaveAsync(user);
                    }

                    await _outgoingClient.SendMessageAsync(
                        message.Channel,
                        message.ExternalChatId,
                        "Got it! Thanks for sharing 🙌");
                }
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
                    "⚠️ Something went wrong. Please try again.");
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
