# Phase 4 — Prompt version history for Persona prompt extensions

## Goal

Extend **prompt version archiving** so that changes to Persona **prompt extension** fields (chat output schema, judge/lead instructions and schemas) produce rows in `PromptVersions`, mirroring existing behavior for system/judge/lead **template** prompts on `PromptSections`.

PostgreSQL remains the **canonical** database; `PromptVersions.PromptType` is stored as an **integer** (no column type change). SQLite dev/test databases use the same model.

## `PromptType` values

| Value | Name | What is versioned |
|------:|------|-------------------|
| 0 | `System` | Persona chat instruction (`Prompts.System`) |
| 1 | `Judge` | Full judge **template** string (`Prompts.Judge`) |
| 2 | `Lead` | Full lead **template** string (`Prompts.Lead`) |
| 3 | `ChatOutputSchema` | `Persona.ChatOutputSchemaJson` |
| 4 | `JudgeInstruction` | `Persona.JudgeInstruction` |
| 5 | `JudgeSchema` | `Persona.JudgeSchemaJson` |
| 6 | `LeadInstruction` | `Persona.LeadInstruction` |
| 7 | `LeadSchema` | `Persona.LeadSchemaJson` |

Legacy `Judge` / `Lead` enum values keep their numeric assignments for **backward compatibility** with existing `PromptVersions` rows.

## Implementation notes

- **`PromptVersionRecorder.RecordPersonaPromptExtensionsIfChangedAsync`** compares prior tracked Persona extension values with the incoming `PersonaPromptExtensionsUpdate` and calls **`AppendIfChangedAsync`** per extension dimension, using the new `PromptType` values above.
- **`EfPersonaRepository.UpdateAsync`** invokes that recorder **before** `ReplacePromptExtensionFields` when `promptExtensions.Apply` is `true`, so the archived `Content` is the **previous** value (or normalized empty), consistent with existing system/judge/lead recording.
- **Behavior** had separate judge/lead extension columns during Phases 2–4; **Phase 5** removed them from the aggregate (see `PHASE5_BEHAVIOR_PROMPT_REMOVAL.md`).

## API / rollout

- Clients that **omit** `promptExtensions` on `PUT /admin/personas/{id}` continue to leave extension columns unchanged (`PersonaPromptExtensionsUpdate` not applied).
- When `promptExtensions` is present, the admin layer sets **`Apply: true`**, so all extension fields in the payload replace stored values (same contract as Phase 2/3).

## Migrations

These migrations update EF **model snapshots** only; **`Up`/`Down` are empty** because the database already stores `PromptType` as `integer`:

- **PostgreSQL:** `Persistence/Migrations/PostgreSql/*ExtendPromptTypeEnumPhase4*`
- **SQLite (`AiEmployeeDbContext`):** `Persistence/Migrations/*ExtendPromptTypeEnumPhase4Sqlite*`

Apply both as part of normal deploys / local `MigrateAsync` so histories stay aligned with the domain model.

## Tests

Integration test **`Put_persona_prompt_extensions_appends_prompt_version_rows_per_extension_prompt_type`** verifies that a persona update with distinct extension values increases the latest `PromptVersions.Version` for each extension-related `PromptType`.
