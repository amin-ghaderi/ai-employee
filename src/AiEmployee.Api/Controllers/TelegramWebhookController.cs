using AiEmployee.Api.Models;
using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Services;
using AiEmployee.Application.UseCases;
using AiEmployee.Domain.Entities;
using AiEmployee.Infrastructure.Telegram;
using Microsoft.AspNetCore.Mvc;

namespace AiEmployee.Api.Controllers;

[ApiController]
[Route("api/telegram")]
public class TelegramWebhookController : ControllerBase
{
    private readonly JudgeUseCase _judgeUseCase;
    private readonly ITelegramClient _telegramClient;
    private readonly IConversationRepository _conversationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILeadRepository _leadRepository;
    private readonly AutomationService _automationService;
    private readonly LeadClassificationService _leadClassificationService;
    private readonly ILogger<TelegramWebhookController> _logger;

    public TelegramWebhookController(
        JudgeUseCase judgeUseCase,
        ITelegramClient telegramClient,
        IConversationRepository conversationRepository,
        IUserRepository userRepository,
        ILeadRepository leadRepository,
        AutomationService automationService,
        LeadClassificationService leadClassificationService,
        ILogger<TelegramWebhookController> logger)
    {
        _judgeUseCase = judgeUseCase;
        _telegramClient = telegramClient;
        _conversationRepository = conversationRepository;
        _userRepository = userRepository;
        _leadRepository = leadRepository;
        _automationService = automationService;
        _leadClassificationService = leadClassificationService;
        _logger = logger;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] TelegramUpdate? update)
    {
        try
        {
            if (update?.Message?.Chat is null || string.IsNullOrWhiteSpace(update.Message.Text))
            {
                return Ok();
            }

            var chatId = update.Message.Chat.Id;
            var text = update.Message.Text.Trim();
            var conversationId = chatId.ToString();
            var messageUserId = update.Message.From?.Id.ToString() ?? conversationId;
            var username = update.Message.From?.Username;
            var firstName = update.Message.From?.FirstName;
            var lastName = update.Message.From?.LastName;

            var user = await _userRepository.GetByIdAsync(messageUserId)
                ?? new User(messageUserId);
            user.UpdateProfile(username, firstName, lastName);
            user.RegisterMessage();
            await _userRepository.SaveAsync(user);

            _logger.LogInformation(
                "User updated: {UserId}, Messages={Count}",
                user.Id,
                user.MessagesCount);

            var commandToken = text.Split(' ')[0];
            commandToken = commandToken.Split('@')[0];
            var isJudgeCommand = string.Equals(commandToken, "/judge", StringComparison.OrdinalIgnoreCase);

            async Task RunAutomationAsync()
            {
                var actions = _automationService.Evaluate(user);
                foreach (var action in actions)
                {
                    if (action == "send_reactivation_message")
                    {
                        await _telegramClient.SendMessageAsync(chatId,
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
                var onboardingMessage = new Message(messageUserId, text, username, firstName, lastName);
                onboardingConversation.AddMessage(onboardingMessage);
                await _conversationRepository.SaveAsync(onboardingConversation);

                await _telegramClient.SendMessageAsync(chatId,
                    "Hey! Quick question — what is your goal?");
                return Ok();
            }

            var command = commandToken;

            _logger.LogInformation("Incoming text: {Text}", text);
            _logger.LogInformation("Parsed command: {Command}", command);

            if (isJudgeCommand)
            {
                var existing = await _conversationRepository.GetByIdAsync(conversationId);
                if (existing is null || existing.Messages.Count == 0)
                {
                    await _telegramClient.SendMessageAsync(chatId, "No conversation found.");
                    await RunAutomationAsync();
                    return Ok();
                }

                var lastMessages = existing.Messages
                    .TakeLast(10)
                    .ToList();

                var participants = new Dictionary<string, string>();
                var nextLabel = 'A';

                string GetFallbackLabel(string userId)
                {
                    if (!participants.ContainsKey(userId))
                    {
                        participants[userId] = nextLabel.ToString();
                        nextLabel++;
                    }

                    return participants[userId];
                }

                string BuildDisplayName(Message m)
                {
                    var hasFirst = !string.IsNullOrWhiteSpace(m.FirstName);
                    var hasLast = !string.IsNullOrWhiteSpace(m.LastName);
                    var hasUsername = !string.IsNullOrWhiteSpace(m.Username);

                    if (hasFirst && hasLast && hasUsername)
                        return $"{m.FirstName} {m.LastName} ({m.Username})";

                    if (hasFirst && hasLast)
                        return $"{m.FirstName} {m.LastName}";

                    if (hasFirst && hasUsername)
                        return $"{m.FirstName} ({m.Username})";

                    if (hasFirst)
                        return m.FirstName!;

                    if (hasUsername)
                        return m.Username!;

                    return GetFallbackLabel(m.UserId);
                }

                var prompt = string.Join("\n", lastMessages.Select(m =>
                {
                    var name = BuildDisplayName(m);
                    return $"{name}: {m.Text}";
                }));

                var conversationResult = await _judgeUseCase.Execute(conversationId, prompt);

                _logger.LogInformation("Sending AI result to Telegram: Winner={Winner}", conversationResult.Winner);

                await _telegramClient.SendMessageAsync(chatId,
                    $"💡 {conversationResult.Reason}");

                await RunAutomationAsync();
                return Ok();
            }

            var conversation = await _conversationRepository.GetByIdAsync(conversationId)
                ?? new Conversation(conversationId);
            var message = new Message(messageUserId, text, username, firstName, lastName);
            conversation.AddMessage(message);
            await _conversationRepository.SaveAsync(conversation);

            _logger.LogInformation("Message saved to conversation {ConversationId}", conversationId);

            var userMessages = conversation.Messages
                .Where(m => m.UserId == messageUserId)
                .OrderBy(m => m.CreatedAt)
                .ToList();

            if (userMessages.Count == 2)
            {
                await _telegramClient.SendMessageAsync(chatId,
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

                    await _telegramClient.SendMessageAsync(chatId,
                        "Got it! Thanks for sharing 🙌");
                }
            }

            await RunAutomationAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telegram webhook processing failed");
            return Ok();
        }
    }
}
