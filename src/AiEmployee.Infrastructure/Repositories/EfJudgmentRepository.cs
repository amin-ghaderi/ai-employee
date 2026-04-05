using AiEmployee.Application.Interfaces;
using AiEmployee.Domain.Entities;
using AiEmployee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiEmployee.Infrastructure.Repositories;

public sealed class EfJudgmentRepository : IJudgmentRepository
{
    private readonly AiEmployeeDbContext _db;

    public EfJudgmentRepository(AiEmployeeDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(Judgment judgment)
    {
        ArgumentNullException.ThrowIfNull(judgment);

        if (!await _db.Conversations.AnyAsync(c => c.Id == judgment.ConversationId))
            _db.Conversations.Add(new Conversation(judgment.ConversationId));

        if (!await _db.Users.AnyAsync(u => u.Id == judgment.UserId))
            _db.Users.Add(new User(judgment.UserId));

        _db.Judgments.Add(judgment);
        await _db.SaveChangesAsync();
    }
}
