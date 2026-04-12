using AiEmployee.Application.Interfaces;
using AiEmployee.Domain.Settings;
using AiEmployee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiEmployee.Infrastructure.Repositories;

public sealed class EfSystemSettingsRepository : ISystemSettingsRepository
{
    private readonly AiEmployeeDbContext _db;

    public EfSystemSettingsRepository(AiEmployeeDbContext db)
    {
        _db = db;
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key is required.", nameof(key));

        var trimmed = key.Trim();
        var row = await _db.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == trimmed, cancellationToken)
            .ConfigureAwait(false);

        return row?.Value;
    }

    public async Task SetValueAsync(string key, string? value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key is required.", nameof(key));

        var trimmedKey = key.Trim();
        var existing = await _db.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == trimmedKey, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(value))
        {
            if (existing is not null)
            {
                _db.SystemSettings.Remove(existing);
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            return;
        }

        var trimmedValue = value.Trim();
        if (trimmedValue.Length > SystemSetting.MaxValueLength)
            throw new ArgumentException($"Value cannot exceed {SystemSetting.MaxValueLength} characters.", nameof(value));

        var now = DateTimeOffset.UtcNow;
        if (existing is null)
        {
            _db.SystemSettings.Add(new SystemSetting(Guid.NewGuid(), trimmedKey, trimmedValue, now));
        }
        else
        {
            existing.UpdateValue(trimmedValue, now);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
