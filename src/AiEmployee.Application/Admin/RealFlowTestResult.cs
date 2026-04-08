namespace AiEmployee.Application.Admin;

public sealed class RealFlowTestResult
{
    public string? FlowExecuted { get; set; }
    public List<CapturedOutgoingMessage> Messages { get; set; } = new();
    public long LatencyMs { get; set; }
    public string? Error { get; set; }
}
