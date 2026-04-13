using AiEmployee.Application.Interfaces;
using AiEmployee.Domain.Entities;
using AiEmployee.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AiEmployee.Infrastructure.Repositories;

public sealed class EfTelegramUpdateDeduplicator : ITelegramUpdateDeduplicator
{
    private readonly AiEmployeeDbContext _db;

    public EfTelegramUpdateDeduplicator(AiEmployeeDbContext db)
    {
        _db = db;
    }

    public Task<bool> TryRegisterFirstDeliveryAsync(
        string botScopeKey,
        long telegramUpdateId,
        CancellationToken cancellationToken = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            try
            {
                _db.ProcessedTelegramUpdates.Add(
                    new ProcessedTelegramUpdate(botScopeKey, telegramUpdateId, DateTimeOffset.UtcNow));
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                _db.ChangeTracker.Clear();
                return false;
            }
        });
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        if (ex.InnerException is PostgresException pg)
            return pg.SqlState == "23505";

        if (ex.InnerException is SqliteException sqlite)
            return sqlite.SqliteExtendedErrorCode == 2067; // SQLITE_CONSTRAINT_UNIQUE

        return false;
    }
}
