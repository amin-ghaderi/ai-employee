namespace AiEmployee.Application.Interfaces;

/// <summary>Reads and writes application-level settings stored in the database.</summary>
public interface ISystemSettingsRepository
{
    /// <summary>Returns the stored value for <paramref name="key"/>, or <c>null</c> when missing.</summary>
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts a setting for <paramref name="key"/>. When <paramref name="value"/> is <c>null</c> or whitespace,
    /// removes the row for that key if it exists.
    /// </summary>
    Task SetValueAsync(string key, string? value, CancellationToken cancellationToken = default);
}
