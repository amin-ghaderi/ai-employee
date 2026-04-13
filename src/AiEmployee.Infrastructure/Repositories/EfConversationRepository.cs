using System.Data;
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

    private bool UseNpgsql() =>
        _db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

    public async Task<Conversation?> GetByIdAsync(string id)
    {
        return await _db.Conversations
            .AsSplitQuery()
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <inheritdoc />
    public async Task AppendUserMessageAsync(
        string conversationId,
        Message message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (string.IsNullOrWhiteSpace(conversationId))
            throw new ArgumentException("Conversation id is required.", nameof(conversationId));

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            var isolation = UseNpgsql() ? IsolationLevel.ReadCommitted : IsolationLevel.Serializable;
            await using var tx = await _db.Database.BeginTransactionAsync(isolation, cancellationToken)
                .ConfigureAwait(false);
            try
            {
                if (UseNpgsql())
                {
                    // Serialize concurrent writers for this chat (row may not exist yet).
                    await _db.Database.ExecuteSqlInterpolatedAsync(
                            $"SELECT pg_advisory_xact_lock(hashtext({conversationId}), 0)",
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                var conv = await _db.Conversations
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken)
                    .ConfigureAwait(false);

                if (conv is null)
                {
                    conv = new Conversation(conversationId);
                    _db.Conversations.Add(conv);
                }

                var alreadyPersisted =
                    conv.Messages.Any(m => m.Id == message.Id)
                    || await _db.Messages.AnyAsync(m => m.Id == message.Id, cancellationToken).ConfigureAwait(false);
                if (alreadyPersisted)
                {
                    await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
                    return;
                }

                var entry = _db.Entry(message);
                if (entry.State != EntityState.Detached)
                    entry.State = EntityState.Detached;

                _db.Messages.Add(message);

                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }).ConfigureAwait(false);
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

            if (await _db.Messages.AnyAsync(m => m.Id == message.Id).ConfigureAwait(false))
                continue;

            var entry = _db.Entry(message);
            if (entry.State != EntityState.Detached)
                entry.State = EntityState.Detached;

            _db.Messages.Add(message);
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

        var toRemove = existing.Messages.ToList();
        _db.Messages.RemoveRange(toRemove);
        existing.Messages.Clear();

        foreach (var m in messages)
        {
            existing.Messages.Add(m);
            _db.Messages.Add(m);
        }

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
