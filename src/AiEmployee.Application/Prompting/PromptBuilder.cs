using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Domain.Entities;

namespace AiEmployee.Application.Prompting;

public sealed class PromptBuilder
{
    public string BuildJudgeTranscript(Conversation conversation, Behavior behavior)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        ArgumentNullException.ThrowIfNull(behavior);

        IEnumerable<Message> messages = conversation.Messages;

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

    public string BuildFullJudgePrompt(
        Conversation conversation,
        Behavior behavior,
        Persona persona,
        PromptTemplate wrapperTemplate)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        ArgumentNullException.ThrowIfNull(behavior);
        ArgumentNullException.ThrowIfNull(persona);
        ArgumentNullException.ThrowIfNull(wrapperTemplate);

        var transcript = BuildJudgeTranscript(conversation, behavior);
        var wrapped = wrapperTemplate.Template.Replace(PromptTokens.Transcript, transcript, StringComparison.Ordinal);
        return persona.Prompts.Judge.Replace(PromptTokens.Input, wrapped, StringComparison.Ordinal);
    }
}
