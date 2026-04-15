using AiEmployee.Application.Dtos.Personas;
using AiEmployee.Domain.BotConfiguration;

namespace AiEmployee.Application.Personas;

public static class PersonaMapper
{
    public static PersonaDto ToDto(Persona persona)
    {
        return new PersonaDto
        {
            Id = persona.Id,
            DisplayName = persona.DisplayName,
            Prompts = new PromptSectionsDto
            {
                System = persona.Prompts.System,
                Judge = persona.Prompts.Judge,
                Lead = persona.Prompts.Lead,
            },
            ClassificationSchema = new ClassificationSchemaDto
            {
                UserTypes = persona.ClassificationSchema.UserTypes.ToList(),
                Intents = persona.ClassificationSchema.Intents.ToList(),
                Potentials = persona.ClassificationSchema.Potentials.ToList(),
            },
            PromptExtensions = new PersonaPromptExtensionsDto
            {
                ChatOutputSchemaJson = persona.ChatOutputSchemaJson,
                JudgeInstruction = persona.JudgeInstruction,
                JudgeSchemaJson = persona.JudgeSchemaJson,
                LeadInstruction = persona.LeadInstruction,
                LeadSchemaJson = persona.LeadSchemaJson,
            },
        };
    }

    public static Persona ToDomain(Guid id, CreatePersonaRequest request)
    {
        var ext = request.PromptExtensions;
        return new Persona(
            id,
            request.DisplayName,
            new PromptSections(
                request.Prompts.System,
                request.Prompts.Judge,
                request.Prompts.Lead),
            new ClassificationSchema(
                (request.ClassificationSchema.UserTypes ?? Array.Empty<string>()).ToList(),
                (request.ClassificationSchema.Intents ?? Array.Empty<string>()).ToList(),
                (request.ClassificationSchema.Potentials ?? Array.Empty<string>()).ToList()),
            ext?.ChatOutputSchemaJson,
            ext?.JudgeInstruction,
            ext?.JudgeSchemaJson,
            ext?.LeadInstruction,
            ext?.LeadSchemaJson);
    }
}
