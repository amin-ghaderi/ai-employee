namespace AiEmployee.Application.Bots;

public sealed class BotValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public BotValidationException(IReadOnlyList<string> errors)
        : base(string.Join("; ", errors))
    {
        Errors = errors;
    }
}
