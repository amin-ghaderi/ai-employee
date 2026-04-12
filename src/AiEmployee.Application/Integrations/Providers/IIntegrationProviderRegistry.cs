namespace AiEmployee.Application.Integrations.Providers;

/// <summary>Resolves <see cref="IIntegrationProvider"/> by canonical provider id (case-insensitive).</summary>
public interface IIntegrationProviderRegistry
{
    IIntegrationProvider? Resolve(string? provider);
}
