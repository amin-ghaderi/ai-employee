namespace AiEmployee.Domain.BotConfiguration;

public sealed class PromptVersion
{
    public Guid Id { get; private set; }
    public Guid PersonaId { get; private set; }
    public PromptType PromptType { get; private set; }
    public int Version { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public string? CreatedBy { get; private set; }

    private PromptVersion()
    {
    }

    public PromptVersion(
        Guid id,
        Guid personaId,
        PromptType promptType,
        int version,
        string content,
        DateTime createdAt,
        string? createdBy)
    {
        Id = id;
        PersonaId = personaId;
        PromptType = promptType;
        Version = version;
        Content = content;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }
}
