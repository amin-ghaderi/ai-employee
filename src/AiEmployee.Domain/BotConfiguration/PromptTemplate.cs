namespace AiEmployee.Domain.BotConfiguration;

public sealed class PromptTemplate
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Template { get; private set; } = string.Empty;

    private PromptTemplate()
    {
    }

    public PromptTemplate(Guid id, string name, string template)
    {
        Id = id;
        Name = name;
        Template = template;
    }
}
