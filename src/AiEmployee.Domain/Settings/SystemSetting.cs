namespace AiEmployee.Domain.Settings;

/// <summary>Application-level key/value setting persisted for runtime configuration (e.g. public base URL).</summary>
public sealed class SystemSetting
{
    public Guid Id { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string? Value { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public const int MaxKeyLength = 128;
    public const int MaxValueLength = 2048;

    private SystemSetting()
    {
    }

    public SystemSetting(Guid id, string key, string? value, DateTimeOffset updatedAtUtc)
    {
        ValidateKey(key);
        ValidateValueLength(value);

        Id = id;
        Key = key.Trim();
        Value = value;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void UpdateValue(string? value, DateTimeOffset updatedAtUtc)
    {
        ValidateValueLength(value);
        Value = value;
        UpdatedAtUtc = updatedAtUtc;
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key is required.", nameof(key));
        if (key.Trim().Length > MaxKeyLength)
            throw new ArgumentException($"Key cannot exceed {MaxKeyLength} characters.", nameof(key));
    }

    private static void ValidateValueLength(string? value)
    {
        if (value is not null && value.Length > MaxValueLength)
            throw new ArgumentException($"Value cannot exceed {MaxValueLength} characters.", nameof(value));
    }
}
