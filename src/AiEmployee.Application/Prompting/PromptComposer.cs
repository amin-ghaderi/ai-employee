using System.Text;
using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Application.Prompting;

public sealed class PromptComposer
{
    public string BuildChatPrompt(Persona persona, string userInput)
    {
        ArgumentNullException.ThrowIfNull(persona);

        var input = userInput ?? string.Empty;
        var system = persona.Prompts.System;

        if (string.IsNullOrWhiteSpace(system))
            return input;

        return
            $"[SYSTEM]{Environment.NewLine}"
            + $"{system.Trim()}{Environment.NewLine}"
            + Environment.NewLine
            + $"[USER]{Environment.NewLine}"
            + $"User: {input}";
    }

    public string BuildHybridChatPrompt(
        Persona persona,
        IReadOnlyList<string> retrievedContextLines,
        IReadOnlyList<string> historyLines,
        string latestUserMessage)
    {
        ArgumentNullException.ThrowIfNull(persona);
        ArgumentNullException.ThrowIfNull(retrievedContextLines);
        ArgumentNullException.ThrowIfNull(historyLines);

        var input = latestUserMessage ?? string.Empty;
        var system = persona.Prompts.System;

        if (string.IsNullOrWhiteSpace(system))
        {
            return BuildChatPromptWithoutSystem(retrievedContextLines, historyLines, input);
        }

        var sb = new StringBuilder();
        sb.Append("[SYSTEM]").AppendLine().AppendLine(system.Trim()).AppendLine();

        if (retrievedContextLines.Count > 0)
        {
            sb.Append("[RETRIEVED CONTEXT]").AppendLine();
            foreach (var line in retrievedContextLines)
                sb.AppendLine(line);
            sb.AppendLine();
        }

        if (historyLines.Count > 0)
        {
            sb.Append("[RECENT CONVERSATION HISTORY]").AppendLine();
            foreach (var line in historyLines)
                sb.AppendLine(line);
            sb.AppendLine();
        }

        sb.Append("[USER]").AppendLine().Append("User: ").Append(input);
        return sb.ToString();
    }

    private static string BuildChatPromptWithoutSystem(
        IReadOnlyList<string> retrievedContextLines,
        IReadOnlyList<string> historyLines,
        string latestUserMessage)
    {
        var sb = new StringBuilder();
        if (retrievedContextLines.Count > 0)
        {
            sb.Append("[RETRIEVED CONTEXT]").AppendLine();
            foreach (var line in retrievedContextLines)
                sb.AppendLine(line);
            sb.AppendLine();
        }

        if (historyLines.Count > 0)
        {
            sb.Append("[RECENT CONVERSATION HISTORY]").AppendLine();
            foreach (var line in historyLines)
                sb.AppendLine(line);
            sb.AppendLine();
        }

        sb.Append("[USER]").AppendLine().Append("User: ").Append(latestUserMessage);
        return sb.ToString();
    }
}
