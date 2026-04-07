using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Application.Admin;

public interface ILeadExecutionService
{
    Task<LeadExecutionResult> ExecuteWithDebugAsync(
        Persona persona,
        Behavior behavior,
        List<string> answers,
        List<string> answerKeys,
        CancellationToken cancellationToken = default);
}
