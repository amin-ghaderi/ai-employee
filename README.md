# 🚀 AI Employee Platform

## 📌 Overview

**AI Employee** is an **AI-powered Telegram bot platform**, not a single-purpose chatbot. It combines a **.NET** host (Clean Architecture), a **Python** AI microservice, and the **Telegram Bot API** to run conversations, judgments, user intelligence, automation, and lead capture in one extensible system.

**What it does**

- Accepts Telegram updates via webhook, persists conversation context (in-memory for the MVP), maintains per-user profiles, and optionally calls an LLM for judgment and lead classification.

**Why it exists**

- To experiment with **structured AI workflows** (judge, classify) behind a real-time channel (Telegram), with clear boundaries between domain rules, application orchestration, and infrastructure.

**Key idea**

- **AI is a tool** invoked through well-defined ports (`IAiClient`), while **business rules** (profiling, automation guards, lead heuristics) live in the domain and application layers. Telegram is one delivery channel; the same core could support other surfaces later.

---

## 🧠 Features

### 🤖 AI Judge

- **`/judge`** (supports `/judge@BotName` in groups) loads recent messages from the chat’s conversation, builds a labeled transcript, and asks the AI for a structured decision (**winner + reason**).
- Normal messages **do not** trigger the judge; only the command does, so groups are not spammed on every line.

### 💬 Conversation Tracking

- Messages are stored **per Telegram chat** (`conversationId` ← chat id).
- Each message carries **user id**, optional **username**, **first/last name** (when Telegram sends them), and **text**, so prompts can show human-readable speakers.

### 👤 User Profiling

- Per-user **message counts**, **engagement score**, and **tags**, including: `new`, `active`, `inactive`, `high_engagement`, plus automation markers (`inactive_notified`, `high_engagement_notified`) and `hot_lead` when applicable.
- Profile is updated on **every handled message** before downstream flows run.

### ⚙️ Automation Engine

- **Rule-based** actions derived from tags (implemented in `AutomationService`).
- Examples:
  - **Inactive** → optional **reactivation** Telegram message (fired once per user while inactive, via `inactive_notified`).
  - **High engagement** → **admin notification** (log / hook; once per marker via `high_engagement_notified`).

### 💰 Lead Engine (AI-based)

- **Onboarding-style capture**: after enough messages from the same user, the **last two** user lines are treated as **goal** and **experience** (configurable flow in code).
- A **Lead** is created **once per user** (guarded by existing leads), then **`LeadClassificationService`** builds a prompt and calls **`IAiClient.ClassifyLeadAsync`**.
- **High potential** leads can receive the **`hot_lead`** tag and a short confirmation in chat.

---

## 🏗 Architecture

The .NET solution follows **Clean Architecture**:

| Layer | Responsibility |
|--------|----------------|
| **Domain** | Pure **entities** (`User`, `Conversation`, `Message`, `Lead`, `JudgmentResult`, …) — no frameworks, no infrastructure. |
| **Application** | **Use cases** (`JudgeUseCase`), **ports** (`IAiClient`, repository interfaces), **DTOs**, and **services** (`AutomationService`, `LeadClassificationService`). |
| **Infrastructure** | **Adapters**: HTTP AI client, in-memory repositories, Telegram client, settings binding. |
| **API** | **Composition root** (`Program.cs`), **controllers**, and Telegram-specific **request models**. |

**AI service (Python)**

- **FastAPI** app (`ai-service/`) exposes HTTP endpoints used by `AiClient` (e.g. judge, and lead classification when configured). The LLM is reached through **Ollama** (local or cloud model via `OLLAMA_MODEL` / `OLLAMA_BASE_URL`).

**Telegram**

- Webhook hits **`POST /api/telegram/webhook`** (`TelegramWebhookController`). A simpler test route exists under **`TelegramController`** for manual JSON posts.

---

## 🔄 How It Works

1. **Telegram** delivers an update to the .NET API webhook.
2. The API **loads or creates** a `User`, applies **profile** fields, **`RegisterMessage()`** (engagement + tags), and **saves** the user.
3. **First message** (non-`/judge`): optional **onboarding** prompt (“what is your goal?”) and early return.
4. **`/judge`**: load conversation → build prompt → **`JudgeUseCase`** → **`IAiClient`** → reply in Telegram.
5. **Otherwise**: append **`Message`** to **`Conversation`** and save.
6. **Lead path** (when eligible and no lead yet): build answers from last user lines → **classify** via AI → save **Lead** → optional **`hot_lead`** + confirmation message.
7. **Automation** runs **after** those state changes: **`AutomationService.Evaluate`**, side effects (e.g. Telegram / logs), then **user saved** again if tags changed.

---

## 🧪 Tech Stack

- **.NET** (C#), ASP.NET Core Web API  
- **FastAPI** (Python) for the AI HTTP service  
- **Ollama** for LLM inference (e.g. cloud models such as `gpt-oss:120b-cloud`, overridable via env)  
- **Telegram Bot API** (`sendMessage`, webhooks)  
- **In-memory** repositories (MVP) — no PostgreSQL/SQLite in the default .NET host for conversations/users/leads  

---

## ⚠️ Current Limitations

- **No durable database** for .NET-side conversation/user/lead stores (restart loses in-memory state unless you add persistence).
- **Lead capture** is **heuristic** (message history / last lines), not a full conversational state machine.
- **No admin dashboard** or authenticated back-office UI.
- **Automation** is intentionally simple (tag-driven, one-shot notification tags); no rich scheduling, rate limits, or multi-channel fan-out.

---

## 🔮 Future Improvements

- **Persistent database** (and migrations) for users, conversations, leads, and judgments.
- **Admin dashboard** (metrics, lead queue, manual overrides).
- **Smarter onboarding** (explicit FSM or LLM-driven slot filling instead of positional heuristics).
- **Stronger intent detection** and multilingual prompts.
- **Background jobs / queues** for classification, notifications, and analytics at scale.

---

## ▶️ Getting Started

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (project targets **.NET 10**)
- Python **3.10+**
- **Ollama** running and reachable (default `http://localhost:11434`), with your chosen model pulled/configured
- A **Telegram bot token** from [@BotFather](https://t.me/BotFather)

### 1. Run the Python AI service

```bash
cd ai-service
pip install -r requirements.txt
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

Set env as needed, e.g. `OLLAMA_MODEL`, `OLLAMA_BASE_URL`, `OLLAMA_TIMEOUT_SECONDS`, and (if used) `DATABASE_URL` for the Python-side judgment history.

### 2. Run the .NET API

```bash
dotnet run --project src/AiEmployee.Api
```

Configure **`Telegram:BotToken`** in `appsettings.json` (prefer **User Secrets** or env in production). Ensure the API URL you expose matches your **webhook** and firewall rules.

### 3. Point Telegram at your webhook

Register the webhook with Telegram (HTTPS required in production), for example:

`https://<your-host>/api/telegram/webhook`

Use `getMe` and `setWebhook` via Telegram’s HTTP API to verify the bot and URL.

### 4. Try it in a group or DM

- Send normal messages → they are **stored**; the bot stays **silent** until onboarding/judge/automation/lead rules apply.
- Send **`/judge`** (or `/judge@YourBot`) → the bot replies with an **AI judgment** when conversation history exists.

---

## 🧩 Project Structure

```
src/
  AiEmployee.Domain/        # Entities only — no external deps
  AiEmployee.Application/   # Interfaces, DTOs, use cases, app services
  AiEmployee.Infrastructure/# AiClient, Telegram, in-memory repos
  AiEmployee.Api/           # Program.cs, controllers, API models

ai-service/                 # FastAPI + Ollama client for LLM endpoints

tests/                      # Unit & integration tests
```

---

## 💡 Philosophy

- **Start with an MVP**: in-memory stores and simple heuristics prove the flow before hardening operations.
- **Clean Architecture first**: keep **domain pure**, depend inward, and plug in AI/Telegram via interfaces.
- **AI as a capability**: prompts and HTTP calls stay at the edges; core policies remain testable C#.
- **Iterate safely**: small, reviewable changes over big rewrites.

---

## 🧑‍💻 Contributing

- Respect **layer boundaries** (Domain ← Application ← Infrastructure; API wires implementations).
- Keep the **domain** free of EF, HTTP, and Telegram SDKs.
- Prefer **small, focused PRs** with clear behavior and updated tests when behavior changes.

---

*Built as an extensible foundation for AI-assisted collaboration over Telegram—not a toy echo bot.*
