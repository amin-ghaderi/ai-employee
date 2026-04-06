namespace AiEmployee.Application.Personas;

public sealed class PersonaValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public PersonaValidationException(IReadOnlyList<string> errors)
        : base(string.Join("; ", errors))
    {
        Errors = errors;
    }
}
