namespace AiEmployee.Application.Dtos.Settings;

/// <summary>Request body for updating the database override for <c>App:PublicBaseUrl</c>.</summary>
public sealed record UpdatePublicBaseUrlRequest(string? PublicBaseUrl);
