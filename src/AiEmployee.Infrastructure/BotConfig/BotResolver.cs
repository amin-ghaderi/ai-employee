using AiEmployee.Application.BotConfig;
using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Messaging;

namespace AiEmployee.Infrastructure.BotConfig;

public sealed class BotResolver : IBotResolver
{
    private readonly IBotConfigurationRepository _repository;

    public BotResolver(IBotConfigurationRepository repository)
    {
        _repository = repository;
    }

    public Task<JudgeBotConfiguration> ResolveAsync(IncomingMessage message)
    {
        var integrationExternalId = Meta(message.Metadata, IncomingMessageMetadataKeys.IntegrationExternalId) ?? string.Empty;
        return _repository.GetByIntegrationAsync(message.Channel, integrationExternalId);
    }

    private static string? Meta(IReadOnlyDictionary<string, string>? metadata, string key) =>
        metadata is not null && metadata.TryGetValue(key, out var v) ? v : null;
}
