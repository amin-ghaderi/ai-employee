using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AiEmployee.Application.Interfaces;
using Json.Schema;

namespace AiEmployee.Infrastructure.AI;

public sealed class JsonSchemaChatOutputValidator : IChatOutputSchemaValidator
{
    public string? TryValidate(string jsonInstance, string schemaJson)
    {
        try
        {
            var schema = JsonSchema.FromText(schemaJson);
            var instance = JsonNode.Parse(jsonInstance);
            var result = schema.Evaluate(instance, new EvaluationOptions
            {
                OutputFormat = OutputFormat.List,
            });

            if (result.IsValid)
                return null;

            return SummarizeErrors(result);
        }
        catch (JsonException ex)
        {
            return "Schema or instance JSON error: " + ex.Message;
        }
    }

    private static string SummarizeErrors(EvaluationResults result)
    {
        if (result.HasErrors && result.Errors is not null)
        {
            var sb = new StringBuilder(400);
            foreach (var err in result.Errors.Take(12))
            {
                if (sb.Length > 0)
                    sb.Append("; ");
                sb.Append(err);
                if (sb.Length > 380)
                    break;
            }

            if (sb.Length > 0)
                return sb.ToString();
        }

        return "Schema validation failed.";
    }
}
