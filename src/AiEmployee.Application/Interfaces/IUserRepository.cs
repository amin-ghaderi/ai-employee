using AiEmployee.Domain.Entities;

namespace AiEmployee.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(string id);
    Task SaveAsync(User user);
}
