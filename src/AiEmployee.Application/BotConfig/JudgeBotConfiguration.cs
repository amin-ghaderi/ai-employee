using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Application.BotConfig;

public sealed record JudgeBotConfiguration(
    Bot Bot,
    Persona Persona,
    Behavior Behavior,
    LanguageProfile LanguageProfile,
    PromptTemplate WrapperTemplate,
    /// <summary>Telegram Bot API token from BotIntegrations.ExternalId for telegram integrations; otherwise null.</summary>
    string? TelegramBotToken = null);
