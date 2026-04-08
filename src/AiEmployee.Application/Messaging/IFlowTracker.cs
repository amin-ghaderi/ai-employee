namespace AiEmployee.Application.Messaging;

public interface IFlowTracker
{
    void Set(string flow);
    string? Get();
}
