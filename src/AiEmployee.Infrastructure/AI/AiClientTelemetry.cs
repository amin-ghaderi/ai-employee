using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AiEmployee.Infrastructure.AI;

/// <summary>OpenTelemetry-style primitives for AI calls (no SQLite or provider-specific logic).</summary>
public static class AiClientTelemetry
{
    public static readonly ActivitySource ActivitySource = new("AiEmployee.AI", "1.0.0");

    public static readonly Meter Meter = new("AiEmployee.AI", "1.0.0");

    public static readonly Counter<long> ChatSchemaValidationFailures =
        Meter.CreateCounter<long>("ai.chat.schema_validation_failures", description:
            "Count of chat completions rejected because the model output did not satisfy the configured JSON schema.");

    public static readonly Histogram<double> ChatCompletionDurationSeconds =
        Meter.CreateHistogram<double>("ai.chat.completion_duration_seconds", unit: "s", description:
            "End-to-end duration of a chat HTTP call including optional schema validation.");
}
