using AiEmployee.Application.Admin;

namespace AiEmployee.Application.Messaging;

public sealed class FlowTracker : IFlowTracker
{
    private readonly RealFlowTestContext _ctx;

    public FlowTracker(RealFlowTestContext ctx)
    {
        _ctx = ctx;
    }

    public void Set(string flow)
    {
        if (_ctx.IsActive)
            _ctx.FlowExecuted = flow;
    }

    public string? Get() => _ctx.FlowExecuted;
}
