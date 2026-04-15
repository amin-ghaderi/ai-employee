using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace AiEmployee.Application.Prompting;

public sealed class PromptBuilder
{
    private readonly ILogger<PromptBuilder> _logger;
    private readonly BehaviorPromptMapper _behaviorPromptMapper;

    public PromptBuilder(
        ILogger<PromptBuilder> logger,
        BehaviorPromptMapper? behaviorPromptMapper = null)
    {
        _logger = logger;
        _behaviorPromptMapper = behaviorPromptMapper
            ?? new BehaviorPromptMapper(NullLogger<BehaviorPromptMapper>.Instance);
    }

    public string BuildJudgeTranscript(Conversation conversation, Behavior behavior)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        ArgumentNullException.ThrowIfNull(behavior);

        // Judge transcript is user-only; assistant rows (Phase 1+) must not alter judge context.
        IEnumerable<Message> messages = conversation.Messages.Where(m => m.Speaker == MessageSpeaker.User);

        if (behavior.ExcludeCommandsFromJudgeContext
            && !string.IsNullOrEmpty(behavior.JudgeCommandPrefix))
        {
            messages = messages.Where(m =>
                !m.Text.StartsWith(behavior.JudgeCommandPrefix, StringComparison.OrdinalIgnoreCase));
        }

        var contextMessages = messages
            .TakeLast(behavior.JudgeContextMessageCount)
            .Select(m => new Message(
                m.ConversationId,
                m.UserId,
                m.Text.Length > behavior.JudgePerMessageMaxChars
                    ? m.Text.Substring(0, behavior.JudgePerMessageMaxChars)
                    : m.Text,
                m.Username,
                m.FirstName,
                m.LastName))
            .ToList();

        var participants = new Dictionary<string, string>();
        var nextLabel = 'A';

        string GetFallbackLabel(string userId)
        {
            if (!participants.ContainsKey(userId))
            {
                participants[userId] = nextLabel.ToString();
                nextLabel++;
            }

            return participants[userId];
        }

        string BuildDisplayName(Message m)
        {
            var hasFirst = !string.IsNullOrWhiteSpace(m.FirstName);
            var hasLast = !string.IsNullOrWhiteSpace(m.LastName);
            var hasUsername = !string.IsNullOrWhiteSpace(m.Username);

            if (hasFirst && hasLast && hasUsername)
                return $"{m.FirstName} {m.LastName} ({m.Username})";

            if (hasFirst && hasLast)
                return $"{m.FirstName} {m.LastName}";

            if (hasFirst && hasUsername)
                return $"{m.FirstName} ({m.Username})";

            if (hasFirst)
                return m.FirstName!;

            if (hasUsername)
                return m.Username!;

            return GetFallbackLabel(m.UserId);
        }

        return string.Join(
            "\n",
            contextMessages.Select(m =>
            {
                var name = BuildDisplayName(m);
                return $"{name}: {m.Text}";
            }));
    }

    public PromptBuildResult BuildFullJudgePrompt(
        Conversation conversation,
        Behavior behavior,
        Persona persona,
        PromptTemplate wrapperTemplate)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        ArgumentNullException.ThrowIfNull(behavior);
        ArgumentNullException.ThrowIfNull(persona);
        ArgumentNullException.ThrowIfNull(wrapperTemplate);

        var judgeTemplate = _behaviorPromptMapper.BuildJudgePrompt(persona);
        var hasInputPlaceholder = judgeTemplate.Contains(PromptTokens.Input, StringComparison.Ordinal);

        if (!wrapperTemplate.Template.Contains(PromptTokens.Transcript, StringComparison.Ordinal))
        {
            _logger.LogError(
                "BuildFullJudgePrompt: Wrapper template '{WrapperName}' is missing '{Token}'. Transcript was not injected.",
                wrapperTemplate.Name,
                PromptTokens.Transcript);
            throw new InvalidOperationException(PromptTokens.JudgeWrapperMissingTranscriptMessage);
        }

        var transcript = BuildJudgeTranscript(conversation, behavior);
        var wrapped = wrapperTemplate.Template.Replace(
            PromptTokens.Transcript,
            transcript,
            StringComparison.Ordinal);

        _logger.LogInformation(
            "BuildFullJudgePrompt: PersonaId={PersonaId}, WrapperName={WrapperName}, JudgeHasInputPlaceholder={HasInput}, TranscriptLength={TranscriptLen}, WrappedLength={WrappedLen}",
            persona.Id,
            wrapperTemplate.Name,
            hasInputPlaceholder,
            transcript.Length,
            wrapped.Length);

        var merged = judgeTemplate.Replace(PromptTokens.Input, wrapped, StringComparison.Ordinal);
        if (ReferenceEquals(merged, judgeTemplate))
        {
            _logger.LogError(
                "BuildFullJudgePrompt: PersonaId={PersonaId} judge prompt is missing '{Token}'. Transcript was not injected.",
                persona.Id,
                PromptTokens.Input);
            throw new InvalidOperationException(
                $"Judge prompt for persona {persona.Id} is missing '{PromptTokens.Input}'. Transcript was not injected.");
        }

        var promptHash = PromptHashing.ComputeSha256(merged);

        _logger.LogInformation(
            "BuildFullJudgePrompt: PersonaId={PersonaId}, WrapperName={WrapperName}, FinalPromptLength={FinalLen}, TranscriptLength={TranscriptLen}, TranscriptEmpty={TranscriptEmpty}, PromptHash={PromptHash}",
            persona.Id,
            wrapperTemplate.Name,
            merged.Length,
            transcript.Length,
            string.IsNullOrEmpty(transcript),
            promptHash);

        return new PromptBuildResult(merged, promptHash);
    }
}
