namespace AiEmployee.Application.Interfaces;

/// <summary>
/// Validates assistant chat JSON against a JSON Schema (draft 6+). Used when the persona has <c>ChatOutputSchemaJson</c> set.
/// </summary>
public interface IChatOutputSchemaValidator
{
    /// <summary>Returns null when the instance satisfies the schema; otherwise a concise, log-safe summary.</summary>
    string? TryValidate(string jsonInstance, string schemaJson);
}
