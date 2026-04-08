namespace AiEmployee.Application.Admin;

public sealed class RealFlowTestRequest
{
    public string Text { get; set; } = default!;
    public string Channel { get; set; } = default!;
    public string ExternalUserId { get; set; } = default!;
    public string ExternalChatId { get; set; } = default!;
    public string? IntegrationExternalId { get; set; }

    public bool ResetConversation { get; set; } = true;
    public bool DisableAutomation { get; set; } = true;
}
