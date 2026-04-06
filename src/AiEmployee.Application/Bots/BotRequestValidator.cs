using AiEmployee.Application.Dtos.Bots;

namespace AiEmployee.Application.Bots;

public static class BotRequestValidator
{
    public static void Validate(CreateBotRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add("name must not be null or empty.");

        if (errors.Count > 0)
            throw new BotValidationException(errors);
    }

    public static void Validate(UpdateBotRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<string>();

        if (request.Name is not null && string.IsNullOrWhiteSpace(request.Name))
            errors.Add("name must not be empty when provided.");

        if (errors.Count > 0)
            throw new BotValidationException(errors);
    }
}
