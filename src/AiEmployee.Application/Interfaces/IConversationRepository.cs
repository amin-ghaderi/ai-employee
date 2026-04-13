using AiEmployee.Domain.Entities;

namespace AiEmployee.Application.Interfaces;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(string id);

    Task<Message?> GetMessageByIdAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically appends a user message to the conversation (creates the row if missing).
    /// Uses a database transaction; under PostgreSQL also takes a transaction-scoped advisory lock per chat id.
    /// </summary>
    Task AppendUserMessageAsync(
        string conversationId,
        Message message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically appends an assistant message (same transaction and locking as <see cref="AppendUserMessageAsync"/>).
    /// </summary>
    Task AppendAssistantMessageAsync(
        string conversationId,
        Message message,
        CancellationToken cancellationToken = default);

    Task SaveAsync(Conversation conversation);

    /// <summary>
    /// Replaces all messages for a conversation (creates the row if missing). Used for admin simulations.
    /// </summary>
    Task ReplaceMessagesAsync(string conversationId, IReadOnlyList<Message> messages, CancellationToken cancellationToken = default);
}
