namespace AiEmployee.Application.Admin;

public sealed class LeadExecutionResult
{
    public string? UserType { get; set; }
    public string? Intent { get; set; }
    public string? Potential { get; set; }

    public LeadDebugResponse Debug { get; set; } = new();
}
