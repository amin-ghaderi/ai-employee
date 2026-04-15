namespace AiEmployee.Application.Prompting;

/// <summary>Optional metadata for chat completion: schema enforcement and observability dimensions.</summary>
public sealed record ChatCompletionRequestContext(
    Guid? PersonaId = null,
    string? ConversationId = null,
    string? ChatOutputSchemaJson = null);
