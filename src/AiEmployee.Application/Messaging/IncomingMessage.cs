namespace AiEmployee.Application.Messaging;

public sealed record IncomingMessage(
    string Channel,
    string ExternalUserId,
    string ExternalChatId,
    string Text,
    IReadOnlyDictionary<string, string>? Metadata = null);
