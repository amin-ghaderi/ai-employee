using AiEmployee.Domain.Entities;

namespace AiEmployee.Application.Interfaces;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(string id);
    Task SaveAsync(Conversation conversation);
}
