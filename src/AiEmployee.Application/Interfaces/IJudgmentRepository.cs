using AiEmployee.Domain.Entities;

namespace AiEmployee.Application.Interfaces;

public interface IJudgmentRepository
{
    Task SaveAsync(Judgment judgment);
}
