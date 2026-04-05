# AI Employee Platform

## 1. Overview

**AI Employee** is a **channel-agnostic AI bot platform** built on **Clean Architecture** and **domain-driven design**. It hosts configurable bots that converse across one or more messaging channels, persist state in a relational database, and call a separate **Python HTTP service** for LLM inference.

**Key idea:** Business rules, orchestration, and **prompt construction** live in **.NET**. The Python service is **execution-only**: it receives prepared prompts (or compact payloads) and returns structured results. Channels are pluggable via **adapters** (inbound) and **message senders** (outbound).

**Why this exists:** To **decouple AI and product logic from any single messaging provider**, so the same core can be **reused across platforms** and channels without **vendor lock-in**. Channel details stay at the edges; the center stays stable, testable, and configuration-driven.

**Capabilities:**

- Multi-step **conversation** and **user** state with tagging and automation hooks  
- **Judge** workflow: structured winner/reason from conversation context  
- **Lead capture** and **AI classification** (user type, intent, potential)  
- **Data-driven bot configuration** (persona, behavior, language, prompt templates) resolved per **channel** and **integration** identity  

---

## 2. Architecture Overview

| Layer | Responsibility |
|--------|----------------|
| **API** | HTTP endpoints, composition root (`Program.cs`), DI registration. Parses raw HTTP bodies and delegates to the **incoming message pipeline**. |
| **Application** | Use cases (`JudgeUseCase`, …), **`IncomingMessageHandler`** orchestration, **`IBotResolver`**, messaging abstractions (`IncomingMessage`, `IChannelAdapter`, `IOutgoingMessageClient`, `IChannelMessageSender`), **`PromptBuilder`**, application services (`AutomationService`, `LeadClassificationService`), repository **interfaces**. |
| **Domain** | Entities and value objects: users, conversations, messages, leads, judgments, and **bot configuration** aggregates (`Bot`, `Persona`, `Behavior`, …). No EF, HTTP, or channel SDKs. |
| **Infrastructure** | **EF Core** persistence, migrations, repository implementations, **`BotResolver`** implementation, channel **adapters** and **senders**, AI HTTP client, external API clients. |

**Component diagram (parallel concerns):**

```
[ HTTP request ]
       │
       ▼
[ API controller ] ──► [ IChannelAdapter ] ──► IncomingMessage
                                                    │
                                                    ▼
                                    [ IncomingMessageHandler ]
                                                    │
                         ┌──────────────────────────┼──────────────────────────┐
                         ▼                          ▼                          ▼
                 [ IBotResolver ]          [ Repositories ]            [ JudgeUseCase / Services ]
                         │                          │                          │
                         ▼                          ▼                          ▼
                 [ IBotConfiguration          [ EF Core / SQLite ]      [ IAiClient ──► Python AI ]
                    Repository ]                                                    │
                         │                                                            ▼
                         └──────────────────────────────► [ IOutgoingMessageClient ] ◄── replies
```

---

## 3. Core Concepts

- **Bot** — Logical bot instance: links a **Persona**, **Behavior**, and **LanguageProfile**, plus channel metadata on the entity. Stored in the database.  
- **BotIntegration** — Maps a **channel key** (string) and **external integration id** (e.g. provider token or app id) to a **Bot**. Uniqueness is enforced on `(Channel, ExternalId)`. Enables multiple channels or accounts to share or separate bot configuration.  

**Persona vs Behavior vs LanguageProfile (do not conflate):**

| Concept | Meaning |
|---------|--------|
| **Persona** | **What the bot is** for the model: AI **role and instructions**—system, judge, and lead **prompt bodies** (plus classification schema where applicable). |
| **Behavior** | **What the bot does** in the product: **rules and mechanics**—command prefix, context limits, lead-flow indices, automation rules, thresholds. |
| **LanguageProfile** | **How the bot speaks** to humans: **user-facing copy**—onboarding lines, error strings, judge reply templates, thanks messages, tone-related labels. |

- **PromptTemplate** — Named templates (e.g. transcript **wrapper**) used when assembling judge prompts in .NET.  
- **IncomingMessage** — Normalized inbound event: **Channel**, **ExternalUserId**, **ExternalChatId**, **Text**, optional **Metadata** (e.g. integration key, display names). All channel-specific parsing stops at the adapter; the handler only sees this model.  

---

## 4. Message Processing Flow

**`IncomingMessageHandler`** is the **central orchestration layer**: after an **`IncomingMessage`** is built, **all business logic** (users, conversations, judge, leads, automation, outbound replies) **flows through it**—not through controllers or ad hoc entry points.

**End-to-end pipeline (logical order):**

```
Channel (HTTP payload)
    → IChannelAdapter
    → IncomingMessage
    → IncomingMessageHandler
    → IBotResolver
    → Use cases / services (e.g. JudgeUseCase)
    → OutgoingMessageDispatcher
    → IChannelMessageSender
```

1. An **HTTP webhook** (or future channel endpoint) receives a provider-specific payload.  
2. The API invokes **`IChannelAdapter.Map(...)`**, which returns an **`IncomingMessage`** or `null` (ignored).  
3. **`IIncomingMessageHandler.HandleAsync(IncomingMessage)`** runs **all** business orchestration: user load/save, automation, onboarding, judge command handling, conversation append, lead flow, outbound replies.  
4. **`IBotResolver.ResolveAsync(IncomingMessage)`** reads **channel** and **integration** id from the message and loads **`JudgeBotConfiguration`** via **`IBotConfigurationRepository`** (DB, with fallback rules as implemented).  
5. **Use cases** (e.g. **`JudgeUseCase`**) and services run with the resolved configuration; **outbound** text goes through **`IOutgoingMessageClient`** (**`OutgoingMessageDispatcher`**) to the correct **`IChannelMessageSender`**.  

No controller or middleware should bypass **`IncomingMessage` → `IncomingMessageHandler`** for full bot behavior.

---

## 5. AI System

### Judge prompting modes

Two modes coexist (controlled by application configuration):

1. **Transcript-based** — A compact **transcript** is sent to the AI service; minimal assembly in .NET beyond formatting.  
2. **Full prompt in .NET** — **`PromptBuilder`** loads conversation from storage, applies **Behavior** (e.g. transcript rules), wraps with **DB `PromptTemplate`**, and injects into **Persona** judge text before calling the AI client.

In both cases, **templates and persona copy are owned by .NET and the database**, not by the Python service.

### Other AI paths

- **Judge flow** — Handler builds or forwards context; **`JudgeUseCase`** persists a **Judgment** and calls **`IAiClient`** for the model response.  
- **Lead classification** — **`LeadClassificationService`** prepares a prompt from captured answers and calls **`IAiClient.ClassifyLeadAsync`**.  
- **Python AI service** — FastAPI app exposing HTTP endpoints consumed by **`AiClient`**. It is **execution-only**: inference (e.g. Ollama or compatible backend). It does **not** own product prompt templates or bot configuration.  

---

## 6. Multi-Channel Design

- **`IChannelAdapter`** — Maps a raw request object to **`IncomingMessage?`**. One implementation per inbound channel format.  
- **`IChannelMessageSender`** — Sends a message for **one** channel type: `Channel` property + `SendAsync(externalChatId, text)`. No cross-channel branching inside senders.  
- **`IOutgoingMessageClient`** — Application-facing port: `SendMessageAsync(channel, externalChatId, text)`.  
- **`OutgoingMessageDispatcher`** — Resolves the sender by **channel** string and delegates; unknown channels are logged and skipped (no throw).  

**To add a new channel:**

1. Implement **`IChannelAdapter`** for that provider’s webhook or event shape.  
2. Implement **`IChannelMessageSender`** for that provider’s outbound API.  
3. Register both in **DI** (API composition root).  
4. Add **`BotIntegration`** rows so `(channel, externalId)` resolves to the intended **Bot**.  

**No changes are required in the Application or Domain layers** for a new channel—only Infrastructure (adapter, sender, optional settings) and composition/DI, plus data.

---

## 7. Persistence & Database

- **EF Core** with **SQLite** by default (`ConnectionStrings:DefaultConnection`, e.g. `aiemployee.db`).  
- **Migrations** live under **`Infrastructure/Persistence/Migrations`**.  

**Primary persisted entities:**

| Area | Tables / entities |
|------|-------------------|
| Messaging state | **User**, **Conversation**, **Message** |
| Leads & AI output | **Lead**, **Judgment** |
| Bot configuration | **Bot**, **BotIntegration**, **Persona**, **Behavior**, **LanguageProfile**, **PromptTemplate** |

---

## 8. Bot Configuration System

- Bots and related rows are **defined in the database** and loaded with **no hardcoded persona text** in the handler.  
- **`BotIntegration`** ties an external integration identity (stored as **ExternalId**) and **Channel** string to a **Bot**. Resolution uses **`IBotConfigurationRepository.GetByIntegrationAsync(channel, externalId)`** with documented fallback (e.g. default bot when no row matches).  
- **`IBotResolver`** encapsulates reading **integration** id from **`IncomingMessage.Metadata`** and invoking the repository—keeping orchestration free of repository details.  
- **`BotConfigurationSeeder`** uses **`JudgeBotDefaults`** (domain static factory methods) to insert the default **Judge** bot graph and, when settings provide an integration token, a matching **`BotIntegration`** row—**idempotent** where designed.  

---

## 9. Running the Project

**Requirements**

- [.NET SDK](https://dotnet.microsoft.com/download) matching the solution (e.g. **.NET 10**)  
- **EF Core tools** (optional but recommended for manual migrations): `dotnet tool install --global dotnet-ef` or use the project-local tool manifest if present  
- Python **3.10+** for `ai-service`  
- **Ollama** (or compatible API) reachable from the Python service  
- A reachable **HTTPS URL** for production webhooks (provider-dependent)  

**1. AI service**

```bash
cd ai-service
pip install -r requirements.txt
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

Configure model and base URL via environment variables as documented in `ai-service` (e.g. `OLLAMA_MODEL`, `OLLAMA_BASE_URL`).

**2. Migrations (before or alongside first API start)**

Apply schema **before** relying on the API in production or CI—for example:

```bash
dotnet ef database update --project src/AiEmployee.Infrastructure --startup-project src/AiEmployee.Api
```

The API also runs **`MigrateAsync`** on **startup**, so local development can rely on automatic migration application. Use explicit updates when your deployment policy requires migrations separate from process start.

**3. .NET API**

```bash
dotnet run --project src/AiEmployee.Api
```

**4. Database seeding**

**Default bot configuration is seeded automatically on startup** (after migrations) when applicable—see **`BotConfigurationSeeder`**. Ensure the host can write to the configured SQLite path (or your overridden connection string).

**5. Webhook**

Point your messaging provider’s webhook URL at the deployed API route wired to your **`IChannelAdapter`**. Use the provider’s API to set and verify the webhook and TLS requirements.

**6. Configuration**

- Connection string under **`ConnectionStrings`**  
- AI options under **`Ai`** (e.g. full judge prompt flag)  
- Channel credentials under the appropriate settings section (bound in **`Program.cs`**)  

Prefer **environment variables** or **user secrets** for non-local environments; override `appsettings` with standard ASP.NET Core configuration precedence.

---

## 10. Development Guidelines

- **No Infrastructure dependencies in Application**—reference **interfaces** and **Domain** only; implementations live in Infrastructure.  
- **All inbound bot flows must go through `IncomingMessageHandler`** after normalization—no parallel “shortcut” controllers for the same behavior.  
- **No channel-specific logic outside Infrastructure** (adapters, senders, vendor clients). Application uses **`message.Channel`** as an opaque string.  
- **Bot behavior must be data-driven**—prefer DB-backed Persona, Behavior, LanguageProfile, and templates over hardcoding in handlers or use cases.  
- Add **channels** by introducing **adapters** and **senders**, then registering them in DI—not by branching on channel names inside the handler.  
- **Domain** stays free of EF attributes, HTTP clients, and vendor SDKs.  
- **Prompt and template** changes belong in **data** or **.NET** (`PromptBuilder`, templates)—not in the Python service as the source of truth.  
- Keep **repository access** for bot configuration behind **`IBotResolver`** / **`IBotConfigurationRepository`** implementations.  

---

## 11. Future Extensions

- Additional **channels** (e.g. web chat, mobile push) via new adapters and senders  
- **Admin UI** or **bot builder** for editing personas, behaviors, and templates without deployments  
- Richer **AI** features: memory, RAG, tool calling, multi-model routing  
- **Observability**: structured logging, metrics, tracing across .NET and Python  
- **Horizontal scale**: outbox pattern, queues, and idempotent webhook processing  

---

## 12. Design Principles

- **Channel-agnostic core** — The center speaks **`IncomingMessage`** and string **channels**, not vendor APIs.  
- **Data-driven behavior** — Persona, behavior, language, and templates are persisted and versionable, not buried in code.  
- **Clean Architecture separation** — Dependencies point inward; Infrastructure is replaceable.  
- **AI at the edges** — Models execute behind **`IAiClient`**; policy and prompts stay in .NET and the database.  
- **Extensible pipeline** — New channels and integrations plug in at the edges **without changing** the core **`IncomingMessage` → handler** flow.  
- **Application purity** — The Application layer depends on **ports and domain only**; it has **no channel-specific types** or vendor SDKs.  

---

## 13. Single Source of Truth

Bot configuration is **fully data-driven**: the **database** is the **single source of truth** for bots, integrations, personas, behaviors, language profiles, and prompt templates. **Code orchestrates execution**—loading configuration, applying rules, and calling AI—but **does not duplicate** long-lived bot definition as hardcoded defaults in handlers (aside from idempotent **seeding** for first-run setup).

---

## 14. Failure Handling

The system favors **graceful degradation** where practical:

- **Missing or unmatched integration** — Resolution **falls back** to the **default bot** (see repository fallback behavior) instead of failing the request.  
- **AI / service failure** — Users receive a **generic error message** from configuration; the host avoids surfacing raw provider errors in chat.  
- **Unknown outbound channel** — **`OutgoingMessageDispatcher`** **logs** and **skips** sends when no **`IChannelMessageSender`** is registered for the channel (no throw).  

---

*AI Employee is designed as a maintainable core for multi-channel, configuration-driven AI assistants—not a single-channel demo.*
