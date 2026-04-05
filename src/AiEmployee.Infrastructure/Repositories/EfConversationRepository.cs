using AiEmployee.Application.Interfaces;
using AiEmployee.Domain.Entities;
using AiEmployee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiEmployee.Infrastructure.Repositories;

public sealed class EfConversationRepository : IConversationRepository
{
    private readonly AiEmployeeDbContext _db;

    public EfConversationRepository(AiEmployeeDbContext db)
    {
        _db = db;
    }

    public async Task<Conversation?> GetByIdAsync(string id)
    {
        return await _db.Conversations
            .AsSplitQuery()
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task SaveAsync(Conversation conversation)
    {
        ArgumentNullException.ThrowIfNull(conversation);

        var existing = await _db.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == conversation.Id);

        if (existing is null)
        {
            _db.Conversations.Add(conversation);
            await _db.SaveChangesAsync();
            return;
        }

        var existingIds = existing.Messages.Select(m => m.Id).ToHashSet();
        foreach (var message in conversation.Messages)
        {
            if (existingIds.Contains(message.Id))
                continue;

            existing.Messages.Add(message);
            existingIds.Add(message.Id);
        }

        await _db.SaveChangesAsync();
    }
}
