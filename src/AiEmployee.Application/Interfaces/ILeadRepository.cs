using AiEmployee.Domain.Entities;

namespace AiEmployee.Application.Interfaces;

public interface ILeadRepository
{
    Task SaveAsync(Lead lead);
    Task<IEnumerable<Lead>> GetByUserIdAsync(string userId);
}
