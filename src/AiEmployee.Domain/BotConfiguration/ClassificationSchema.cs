namespace AiEmployee.Domain.BotConfiguration;

public sealed class ClassificationSchema
{
    public IReadOnlyList<string> UserTypes { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> Intents { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> Potentials { get; private set; } = Array.Empty<string>();

    private ClassificationSchema()
    {
    }

    public ClassificationSchema(
        IReadOnlyList<string> userTypes,
        IReadOnlyList<string> intents,
        IReadOnlyList<string> potentials)
    {
        UserTypes = userTypes ?? Array.Empty<string>();
        Intents = intents ?? Array.Empty<string>();
        Potentials = potentials ?? Array.Empty<string>();
    }
}
