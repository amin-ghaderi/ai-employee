using AiEmployee.Application.Interfaces;

namespace AiEmployee.Application.Admin;

public sealed class RealFlowTestContext
{
    public bool IsActive { get; set; }

    public bool DisableAutomation { get; set; } = true;

    public List<CapturedOutgoingMessage> CapturedMessages { get; } = new();

    public string? FlowExecuted { get; set; }

    public IConversationRepository? ConversationOverride { get; set; }

    /// <summary>
    /// When Real Flow is active, set when AI pipeline fails but the handler sends a generic user message instead of throwing.
    /// </summary>
    public string? PipelineError { get; set; }
}

public sealed record CapturedOutgoingMessage(
    string Channel,
    string ExternalChatId,
    string Text,
    DateTimeOffset Timestamp);
