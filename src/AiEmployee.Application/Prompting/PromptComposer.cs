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
}
