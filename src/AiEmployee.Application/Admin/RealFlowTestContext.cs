using AiEmployee.Application.Interfaces;

namespace AiEmployee.Application.Admin;

public sealed class RealFlowTestContext
{
    public bool IsActive { get; set; }

    public bool DisableAutomation { get; set; } = true;

    public List<CapturedOutgoingMessage> CapturedMessages { get; } = new();

    public string? FlowExecuted { get; set; }

    public IConversationRepository? ConversationOverride { get; set; }
}

public sealed record CapturedOutgoingMessage(
    string Channel,
    string ExternalChatId,
    string Text,
    DateTimeOffset Timestamp);
