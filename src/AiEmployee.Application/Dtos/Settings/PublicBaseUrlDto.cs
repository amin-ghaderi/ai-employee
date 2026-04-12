namespace AiEmployee.Application.Dtos.Settings;

/// <summary>Effective public base URL (after database + configuration precedence).</summary>
public sealed record PublicBaseUrlDto(string? PublicBaseUrl);
