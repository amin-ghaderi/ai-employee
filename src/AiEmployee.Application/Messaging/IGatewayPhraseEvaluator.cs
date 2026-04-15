using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Application.Messaging;

public interface IGatewayPhraseEvaluator
{
    bool ShouldRouteToGateway(Behavior behavior, string messageText);
}
