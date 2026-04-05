using System.Diagnostics;
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

    public async Task ReplaceMessagesAsync(
        string conversationId,
        IReadOnlyList<Message> messages,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var existing = await _db.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            var conv = new Conversation(conversationId);
            foreach (var m in messages)
                conv.AddMessage(m);

            _db.Conversations.Add(conv);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        _db.Messages.RemoveRange(existing.Messages);

        foreach (var m in messages)
            _db.Messages.Add(m);

#if DEBUG
        foreach (var m in messages)
        {
            Debug.Assert(
                _db.Entry(m).State == EntityState.Added,
                $"New message {m.Id} must be Added before SaveChanges, was {_db.Entry(m).State}.");
        }
#endif

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
