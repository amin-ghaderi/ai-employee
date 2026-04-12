namespace AiEmployee.Application.Integrations.Providers;

/// <inheritdoc />
public sealed class IntegrationProviderRegistry : IIntegrationProviderRegistry
{
    private readonly IReadOnlyDictionary<string, IIntegrationProvider> _providers;

    public IntegrationProviderRegistry(IEnumerable<IIntegrationProvider> providers)
    {
        _providers = providers.ToDictionary(
            p => p.ProviderId.ToLowerInvariant(),
            p => p);
    }

    /// <inheritdoc />
    public IIntegrationProvider? Resolve(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
            return null;

        _providers.TryGetValue(provider.ToLowerInvariant(), out var result);
        return result;
    }
}
