using AiEmployee.Domain.Entities;

namespace AiEmployee.Application.Interfaces;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(string id);
    Task SaveAsync(Conversation conversation);

    /// <summary>
    /// Replaces all messages for a conversation (creates the row if missing). Used for admin simulations.
    /// </summary>
    Task ReplaceMessagesAsync(string conversationId, IReadOnlyList<Message> messages, CancellationToken cancellationToken = default);
}
