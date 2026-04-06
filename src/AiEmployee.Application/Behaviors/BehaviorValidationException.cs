namespace AiEmployee.Application.Behaviors;

public sealed class BehaviorValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public BehaviorValidationException(IReadOnlyList<string> errors)
        : base(string.Join("; ", errors))
    {
        Errors = errors;
    }
}
