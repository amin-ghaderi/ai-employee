# Phase 2 — Application prompt configuration (Persona-first)

## Summary

> **Phase 5 update:** The four judge/lead extension columns were **removed** from `Behavior` (see `PHASE5_BEHAVIOR_PROMPT_REMOVAL.md`). Runtime assembly is **Persona-only** for those prompts.

Runtime prompt assembly **reads judge/lead instruction + schema from `Persona`**, then uses the full **`Persona.Prompts` templates** when extensions are empty. General chat uses **`BehaviorPromptMapper.BuildChatSystemContent`**, which appends **optional chat output JSON schema** from `Persona.ChatOutputSchemaJson`.

## API / DTO

- `PersonaDto` includes **`promptExtensions`** (`PersonaPromptExtensionsDto`) with: `chatOutputSchemaJson`, `judgeInstruction`, `judgeSchemaJson`, `leadInstruction`, `leadSchemaJson`.
- `CreatePersonaRequest` / `UpdatePersonaRequest` accept optional **`promptExtensions`**.
- **Update semantics:** if `promptExtensions` is **omitted** (`null`), existing extension columns are **unchanged**. If `promptExtensions` is **sent**, all extension fields are **replaced** (empty strings clear after normalization).

## Validation

- `PersonaRequestValidator` validates non-empty `*SchemaJson` and `chatOutputSchemaJson` as **parseable JSON** (invalid JSON → `PersonaValidationException`).

## Dual-read order

1. **Judge:** Persona extensions → `Persona.Prompts.Judge`
2. **Lead:** Persona extensions → `Persona.Prompts.Lead`
3. **Chat system block:** `Persona.Prompts.System` + optional schema appendix from `Persona.ChatOutputSchemaJson`

## Admin debug

- `GetJudgePromptSource` / `GetLeadPromptSource` take **`Persona`** and return `Persona-extensions` or `Persona-template`.
- Effective schema JSON for debug panels uses **`GetEffectiveJudgeSchemaJson` / `GetEffectiveLeadSchemaJson`** (Persona first).

## Migration notes (data)

Phase 1 migrations already **backfilled** Persona extension columns from `Behaviors` where linked via `Bots`. Phase 2 **does not** add new migrations. When deploying:

1. Apply **PostgreSQL** migrations through **`AiEmployeeDbContext`** (canonical; see `docs/PHASE7_POSTGRES_ONLY.md`).
2. Ensure API consumers that **update** personas either omit `promptExtensions` (preserve) or send the full object when replacing values.

## PostgreSQL authority

No SQLite-specific logic was added in the Application layer. Schema evolution remains the responsibility of the **PostgreSQL** migration track; SQLite migrations remain a parallel compatibility mirror only.
