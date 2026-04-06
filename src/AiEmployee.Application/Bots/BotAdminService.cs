using AiEmployee.Application.Dtos.Bots;
using AiEmployee.Application.Interfaces;
using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Application.Bots;

public sealed class BotAdminService : IBotAdminService
{
    private readonly IBotRepository _botRepository;
    private readonly IPersonaRepository _personaRepository;
    private readonly IBehaviorRepository _behaviorRepository;
    private readonly ILanguageProfileRepository _languageProfileRepository;

    public BotAdminService(
        IBotRepository botRepository,
        IPersonaRepository personaRepository,
        IBehaviorRepository behaviorRepository,
        ILanguageProfileRepository languageProfileRepository)
    {
        _botRepository = botRepository;
        _personaRepository = personaRepository;
        _behaviorRepository = behaviorRepository;
        _languageProfileRepository = languageProfileRepository;
    }

    public async Task<BotDto> CreateAsync(CreateBotRequest request, CancellationToken cancellationToken = default)
    {
        BotRequestValidator.Validate(request);

        var id = Guid.NewGuid();
        var bot = new Bot(
            id,
            request.Name,
            BotChannel.Telegram,
            id.ToString("N"),
            JudgeBotDefaults.PersonaId,
            JudgeBotDefaults.BehaviorId,
            JudgeBotDefaults.LanguageProfileId,
            request.IsEnabled,
            DateTimeOffset.UtcNow);

        await _botRepository.AddAsync(bot, cancellationToken).ConfigureAwait(false);
        return BotMapper.ToDto(bot);
    }

    public async Task<BotDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var bot = await _botRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return bot is null ? null : BotMapper.ToDto(bot);
    }

    public async Task<IReadOnlyList<BotDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await _botRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        return list.Select(BotMapper.ToDto).ToList();
    }

    public async Task<BotDto> UpdateAsync(Guid id, UpdateBotRequest request, CancellationToken cancellationToken = default)
    {
        BotRequestValidator.Validate(request);
        var existing = await _botRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
            throw new KeyNotFoundException($"No bot was found for id '{id}'.");

        existing.Update(
            request.Name ?? existing.Name,
            request.IsEnabled ?? existing.IsEnabled,
            DateTimeOffset.UtcNow);

        await _botRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        return BotMapper.ToDto(existing);
    }

    public async Task<BotDto> AssignAsync(Guid id, BotAssignmentsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var existing = await _botRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
            throw new KeyNotFoundException($"No bot was found for id '{id}'.");

        var errors = new List<string>();
        if (request.PersonaId == Guid.Empty)
            errors.Add("personaId must not be empty.");
        if (request.BehaviorId == Guid.Empty)
            errors.Add("behaviorId must not be empty.");
        if (request.LanguageProfileId == Guid.Empty)
            errors.Add("languageProfileId must not be empty.");
        if (errors.Count > 0)
            throw new BotValidationException(errors);

        var persona = await _personaRepository.GetByIdAsync(request.PersonaId, cancellationToken).ConfigureAwait(false);
        if (persona is null)
            errors.Add($"No persona was found for id '{request.PersonaId}'.");

        var behavior = await _behaviorRepository.GetByIdAsync(request.BehaviorId, cancellationToken).ConfigureAwait(false);
        if (behavior is null)
            errors.Add($"No behavior was found for id '{request.BehaviorId}'.");

        var languageProfile = await _languageProfileRepository.GetByIdAsync(request.LanguageProfileId, cancellationToken).ConfigureAwait(false);
        if (languageProfile is null)
            errors.Add($"No language profile was found for id '{request.LanguageProfileId}'.");

        if (errors.Count > 0)
            throw new BotValidationException(errors);

        existing.Assign(
            request.PersonaId,
            request.BehaviorId,
            request.LanguageProfileId,
            DateTimeOffset.UtcNow);

        await _botRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        return BotMapper.ToDto(existing);
    }

    public async Task<BotDto> EnableAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _botRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
            throw new KeyNotFoundException($"No bot was found for id '{id}'.");

        if (existing.PersonaId == Guid.Empty)
            throw new InvalidOperationException("Bot cannot be enabled because PersonaId is not configured.");
        if (existing.BehaviorId == Guid.Empty)
            throw new InvalidOperationException("Bot cannot be enabled because BehaviorId is not configured.");
        if (existing.LanguageProfileId == Guid.Empty)
            throw new InvalidOperationException("Bot cannot be enabled because LanguageProfileId is not configured.");

        var persona = await _personaRepository.GetByIdAsync(existing.PersonaId, cancellationToken).ConfigureAwait(false);
        if (persona is null)
            throw new InvalidOperationException($"Bot cannot be enabled because persona '{existing.PersonaId}' was not found.");

        var behavior = await _behaviorRepository.GetByIdAsync(existing.BehaviorId, cancellationToken).ConfigureAwait(false);
        if (behavior is null)
            throw new InvalidOperationException($"Bot cannot be enabled because behavior '{existing.BehaviorId}' was not found.");

        var languageProfile = await _languageProfileRepository.GetByIdAsync(existing.LanguageProfileId, cancellationToken).ConfigureAwait(false);
        if (languageProfile is null)
            throw new InvalidOperationException($"Bot cannot be enabled because language profile '{existing.LanguageProfileId}' was not found.");

        existing.SetEnabled(true, DateTimeOffset.UtcNow);
        await _botRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        return BotMapper.ToDto(existing);
    }

    public async Task<BotDto> DisableAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _botRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
            throw new KeyNotFoundException($"No bot was found for id '{id}'.");

        existing.SetEnabled(false, DateTimeOffset.UtcNow);
        await _botRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        return BotMapper.ToDto(existing);
    }
}
