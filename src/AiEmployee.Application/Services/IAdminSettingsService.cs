using AiEmployee.Application.Dtos.Settings;

namespace AiEmployee.Application.Services;

/// <summary>Admin operations for application settings stored in the database.</summary>
public interface IAdminSettingsService
{
    /// <summary>Returns the effective public base URL (database override or configuration).</summary>
    Task<PublicBaseUrlDto> GetPublicBaseUrlAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and persists a database override for the public base URL.
    /// When <paramref name="url"/> is null or whitespace, clears the override (same as <see cref="ClearPublicBaseUrlAsync"/>).
    /// </summary>
    Task<PublicBaseUrlDto> SetPublicBaseUrlAsync(string? url, CancellationToken cancellationToken = default);

    /// <summary>Removes the database override so configuration is used.</summary>
    Task ClearPublicBaseUrlAsync(CancellationToken cancellationToken = default);
}
