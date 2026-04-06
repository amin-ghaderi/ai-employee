namespace AiEmployee.Application.Integrations;

public sealed class BotIntegrationValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public BotIntegrationValidationException(IReadOnlyList<string> errors)
        : base(string.Join("; ", errors))
    {
        Errors = errors;
    }
}
