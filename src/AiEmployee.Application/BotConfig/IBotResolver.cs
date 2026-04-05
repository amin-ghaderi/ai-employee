using AiEmployee.Application.Messaging;

namespace AiEmployee.Application.BotConfig;

public interface IBotResolver
{
    /// <summary>
    /// Resolves <see cref="JudgeBotConfiguration"/> using <see cref="IncomingMessage.Channel"/>
    /// and integration external id from <see cref="IncomingMessage.Metadata"/>.
    /// </summary>
    Task<JudgeBotConfiguration> ResolveAsync(IncomingMessage message);
}
