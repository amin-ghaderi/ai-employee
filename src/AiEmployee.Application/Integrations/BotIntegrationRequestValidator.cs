using AiEmployee.Application.Dtos.Integrations;

namespace AiEmployee.Application.Integrations;

public static class BotIntegrationRequestValidator
{
    public static void Validate(CreateBotIntegrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<string>();

        if (request.BotId == Guid.Empty)
            errors.Add("botId must not be empty.");

        if (string.IsNullOrWhiteSpace(request.Channel))
            errors.Add("channel must not be null or empty.");

        if (string.IsNullOrWhiteSpace(request.ExternalId))
            errors.Add("externalId must not be null or empty.");

        if (errors.Count > 0)
            throw new BotIntegrationValidationException(errors);
    }

    public static void Validate(UpdateBotIntegrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<string>();

        if (request.BotId is Guid botId && botId == Guid.Empty)
            errors.Add("botId must not be empty when provided.");

        if (request.Channel is not null && string.IsNullOrWhiteSpace(request.Channel))
            errors.Add("channel must not be empty when provided.");

        if (request.ExternalId is not null && string.IsNullOrWhiteSpace(request.ExternalId))
            errors.Add("externalId must not be empty when provided.");

        if (request.BotId is null && request.Channel is null && request.ExternalId is null && request.IsEnabled is null
            && request.GatewayChannel is null && request.GatewayExternalId is null)
            errors.Add("At least one field must be provided for update.");

        if (errors.Count > 0)
            throw new BotIntegrationValidationException(errors);
    }
}
