# Phase 5 — Remove judge/lead prompt extensions from Behavior

## Goal

Remove **`JudgeInstruction`**, **`JudgeSchemaJson`**, **`LeadInstruction`**, and **`LeadSchemaJson`** from the **`Behavior`** aggregate and database. Judge/lead **instruction + output schema** now live only on **`Persona`** (`promptExtensions` / extension columns). Full **judge/lead templates** remain on **`Persona.Prompts`** (`JudgePrompt` / `LeadPrompt` columns) as before.

`BehaviorPromptMapper` no longer reads Behavior for those fields: it uses **persona extensions** when they produce non-empty combined text, otherwise **`Persona.Prompts.Judge` / `Lead`**.

## Data safety (migrations)

Migrations **`Phase5RemoveBehaviorPromptFields`** / **`Phase5RemoveBehaviorPromptFieldsPostgres`** (historical names; Phase 7 uses a single **`AiEmployeeDbContext`** on PostgreSQL) run **before** `DropColumn`:

1. **Backfill Personas** from the linked **Behavior** (via `Bots`), using the **first bot id order** per persona, **only when** the persona’s judge (respectively lead) extension fields are still “empty” in the same sense as the old dual-read gate (no instruction text and no non-trivial schema JSON).
2. **Drop** the four columns from **`Behaviors`**.

This preserves data for deployments that still had overrides only on **Behavior** after Phase 2/3. Personas that already had extensions set are **not overwritten**.

## API / Admin UI

- **Behavior DTOs and requests** no longer include the four fields.
- **`BehaviorsPage.jsx`** no longer loads or re-sends legacy prompt fields; copy on the page points editors to **Prompt Configuration** for prompts.

## Rollback

`Down` re-adds empty nullable columns; it does **not** restore dropped text from `Personas`.
