# Phase 3 — Admin UI (Prompt Configuration)

## Navigation

- **App**: Tab label **Prompt Configuration** (route state key remains `personas` for minimal churn).
- **Bots** table column header: **Prompt configuration** (still binds `personaId` in API payloads).

## Prompt Configuration page (`PersonasPage.jsx`)

- Page title: **Prompt Configuration**.
- **General chat**: Display name, **Chat instruction** (API: `prompts.system`), **Chat output schema (JSON)** (`promptExtensions.chatOutputSchemaJson`).
- **Judge**: Full **Judge template** (`prompts.judge`), **Judge instruction**, **Judge output schema** (`promptExtensions.*`).
- **Lead**: Full **Lead template** (`prompts.lead`), **Lead instruction**, **Lead output schema**.
- **Classification**: User types, intents, potentials (unchanged).

### Save semantics

- **Create**: `promptExtensions` is sent only when at least one extension field is non-empty (avoids unnecessary replace semantics).
- **Update**: `promptExtensions` is **always** sent so edits and clears persist (matches Phase 2 API contract).

### Helpers

- `promptConfigFormUtils.js` — `normalizeOptionalSchemaJson`, `hasAnyPromptExtensionValue` (pure functions; no SQLite or DB coupling).

## Behaviors page (`BehaviorsPage.jsx`)

- **Removed** UI for judge/lead instruction and JSON schemas (Phase 3).
- **Phase 5:** Behavior API no longer exposes those fields; the page sends **only** behavior configuration (judge context, lead flow, flags, hot lead). Prompts are edited under **Prompt Configuration**.

## Tests

- **Integration**: `AdminPersonaPromptConfigurationUpdateTests` — GET → PUT with `promptExtensions` → GET assert → **restore** original JSON (does not leave test pollution).
- **Integration**: existing `AdminPersonasPromptExtensionsTests` — list response includes `promptExtensions`.
- **Build**: `npm run build` in `admin-ui` (production bundle).

## PostgreSQL

No admin UI change targets SQLite; canonical DB policy unchanged (see `PHASE2_APPLICATION_PROMPT_MIGRATION.md`).
