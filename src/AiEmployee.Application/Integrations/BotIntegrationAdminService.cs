using AiEmployee.Application.Dtos.Integrations;
using AiEmployee.Application.Interfaces;
using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Application.Integrations;

public sealed class BotIntegrationAdminService : IBotIntegrationAdminService
{
    private readonly IBotIntegrationRepository _integrationRepository;
    private readonly IBotRepository _botRepository;

    public BotIntegrationAdminService(
        IBotIntegrationRepository integrationRepository,
        IBotRepository botRepository)
    {
        _integrationRepository = integrationRepository;
        _botRepository = botRepository;
    }

    public async Task<BotIntegrationDto> CreateAsync(
        CreateBotIntegrationRequest request,
        CancellationToken cancellationToken = default)
    {
        BotIntegrationRequestValidator.Validate(request);
        await EnsureBotAllowedAsync(request.BotId, cancellationToken).ConfigureAwait(false);

        var channel = BotIntegrationChannelNames.NormalizeChannelValue(request.Channel);
        var externalId = request.ExternalId.Trim();
        if (string.IsNullOrEmpty(channel) || string.IsNullOrEmpty(externalId))
            throw new BotIntegrationValidationException(new[]
            {
                "channel and externalId must be non-empty after normalization.",
            });

        var integration = new BotIntegration(
            Guid.NewGuid(),
            request.BotId,
            channel,
            externalId,
            request.IsEnabled);

        await _integrationRepository.AddAsync(integration, cancellationToken).ConfigureAwait(false);
        return BotIntegrationMapper.ToDto(integration);
    }

    public async Task<BotIntegrationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var integration = await _integrationRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return integration is null ? null : BotIntegrationMapper.ToDto(integration);
    }

    public async Task<IReadOnlyList<BotIntegrationDto>> ListAsync(
        Guid? botId,
        CancellationToken cancellationToken = default)
    {
        var list = await _integrationRepository.ListAsync(botId, cancellationToken).ConfigureAwait(false);
        return list.Select(BotIntegrationMapper.ToDto).ToList();
    }

    public async Task<BotIntegrationDto> UpdateAsync(
        Guid id,
        UpdateBotIntegrationRequest request,
        CancellationToken cancellationToken = default)
    {
        BotIntegrationRequestValidator.Validate(request);
        var current = await _integrationRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (current is null)
            throw new KeyNotFoundException($"No bot integration was found for id '{id}'.");

        var botId = request.BotId ?? current.BotId;
        if (request.BotId is not null)
            await EnsureBotAllowedAsync(botId, cancellationToken).ConfigureAwait(false);

        string channel;
        string externalId;
        if (request.Channel is not null)
        {
            channel = request.Channel.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(channel))
                throw new BotIntegrationValidationException(new[] { "channel must be non-empty after normalization." });
        }
        else
        {
            channel = current.Channel;
        }

        if (request.ExternalId is not null)
        {
            externalId = request.ExternalId.Trim();
            if (string.IsNullOrEmpty(externalId))
                throw new BotIntegrationValidationException(new[] { "externalId must be non-empty after normalization." });
        }
        else
        {
            externalId = current.ExternalId;
        }

        var isEnabled = request.IsEnabled ?? current.IsEnabled;

        var updated = new BotIntegration(current.Id, botId, channel, externalId, isEnabled);
        await _integrationRepository.UpdateAsync(updated, cancellationToken).ConfigureAwait(false);

        var reloaded = await _integrationRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"No bot integration was found for id '{id}'.");
        return BotIntegrationMapper.ToDto(reloaded);
    }

    public async Task<BotIntegrationDto> EnableAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var current = await _integrationRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (current is null)
            throw new KeyNotFoundException($"No bot integration was found for id '{id}'.");

        var channel = BotIntegrationChannelNames.NormalizeChannelValue(current.Channel);
        var updated = new BotIntegration(current.Id, current.BotId, channel, current.ExternalId, isEnabled: true);
        await _integrationRepository.UpdateAsync(updated, cancellationToken).ConfigureAwait(false);

        var reloaded = await _integrationRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"No bot integration was found for id '{id}'.");
        return BotIntegrationMapper.ToDto(reloaded);
    }

    public async Task<BotIntegrationDto> DisableAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var current = await _integrationRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (current is null)
            throw new KeyNotFoundException($"No bot integration was found for id '{id}'.");

        var channel = BotIntegrationChannelNames.NormalizeChannelValue(current.Channel);
        var updated = new BotIntegration(current.Id, current.BotId, channel, current.ExternalId, isEnabled: false);
        await _integrationRepository.UpdateAsync(updated, cancellationToken).ConfigureAwait(false);

        var reloaded = await _integrationRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"No bot integration was found for id '{id}'.");
        return BotIntegrationMapper.ToDto(reloaded);
    }

    private async Task EnsureBotAllowedAsync(Guid botId, CancellationToken cancellationToken)
    {
        var bot = await _botRepository.GetByIdAsync(botId, cancellationToken).ConfigureAwait(false);
        if (bot is null)
            throw new KeyNotFoundException($"No bot was found for id '{botId}'.");

        if (!bot.IsEnabled)
            throw new BotIntegrationValidationException(new[]
            {
                $"Bot '{botId}' is disabled; integrations cannot be created or pointed to a disabled bot.",
            });
    }
}
