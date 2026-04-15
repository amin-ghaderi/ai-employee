# Phase 6 — Chat output schema enforcement and AI telemetry

## Goals

1. **Enforce** persona `ChatOutputSchemaJson` on general chat completions when configured (JSON Schema, draft 6+).
2. **Validate** in the **AI client layer** (`AiClient`) immediately after the HTTP response is parsed, before the assistant text is returned to use cases.
3. **Observability**: `ActivitySource` spans (`ai.chat`), structured log scopes, counters, and a latency histogram for production monitoring (OpenTelemetry-compatible primitives; no SQLite-specific logic).

## Runtime behavior

- **`AssistantUseCase`** passes `ChatCompletionRequestContext` with `PersonaId`, `ConversationId`, and `ChatOutputSchemaJson` into `IAiClient.ChatAsync`.
- When **`Ai:EnforceChatOutputSchema`** is `true` (default) and the persona defines a **non-trivial** schema (not empty, `{}`, or `null`), the client:
  1. Extracts a JSON object from the model text (`ChatAssistantJsonPayloadExtractor` — supports optional fenced code blocks labeled `json` and leading prose).
  2. Validates the object with **JsonSchema.Net** (`IChatOutputSchemaValidator` / `JsonSchemaChatOutputValidator`).
  3. Maps the validated JSON to user-visible text via **`ChatStructuredResponseFormatter`** (prefers top-level string properties `message`, `reply`, `text`, `content`, `response`, `answer`; otherwise returns compact JSON).

- When **no schema** is configured, or enforcement is **disabled**, behavior matches **Phase 5**: the inner string from the AI service `ChatResponse` is returned unchanged.

## Configuration

`appsettings.json` → **`Ai:EnforceChatOutputSchema`** (default `true`). Set `false` for emergency rollback without editing persona data.

## Telemetry surface

| Instrument | Meaning |
|------------|---------|
| `ActivitySource` name `AiEmployee.AI`, span name `ai.chat` | Tags: `ai.persona.id`, `ai.conversation.id`, `ai.chat.schema.enforced`, `ai.chat.schema.result` (`valid` / `invalid` / `no_json_object`). |
| `Meter` name `AiEmployee.AI` | Counter `ai.chat.schema_validation_failures`; histogram `ai.chat.completion_duration_seconds` (tag `schema_enforced`). |
| Log scopes | `AiOperation`, `PersonaId`, `ConversationId`, `ChatSchemaEnforced` on chat calls. |

Exporters (OTLP, Azure Monitor, etc.) are configured at the host; this phase only emits standard .NET metrics and activities.

## Tests

- **`ChatOutputSchemaPhase6Tests`** — extractor, formatter, and `JsonSchemaChatOutputValidator` happy/failure paths.

## Backward compatibility

Personas **without** `ChatOutputSchemaJson` are unaffected. Personas **with** a schema now get strict validation; operators should ensure prompts and models can emit valid JSON (see `ChatStructuredResponseFormatter` for user-visible field conventions).
