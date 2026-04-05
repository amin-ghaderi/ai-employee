using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Application.BotConfig;

public sealed record JudgeBotConfiguration(
    Bot Bot,
    Persona Persona,
    Behavior Behavior,
    LanguageProfile LanguageProfile,
    PromptTemplate WrapperTemplate);
