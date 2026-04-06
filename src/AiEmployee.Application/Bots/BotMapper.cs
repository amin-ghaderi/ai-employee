using AiEmployee.Application.Dtos.Bots;
using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Application.Bots;

public static class BotMapper
{
    public static BotDto ToDto(Bot bot)
    {
        return new BotDto
        {
            Id = bot.Id,
            Name = bot.Name,
            PersonaId = bot.PersonaId,
            BehaviorId = bot.BehaviorId,
            LanguageProfileId = bot.LanguageProfileId,
            IsEnabled = bot.IsEnabled,
        };
    }
}
