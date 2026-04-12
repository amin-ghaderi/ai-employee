# Technical summary — Telegram & production readiness (Phase 7)

## Platform

**AI Employee** is a .NET 10 + SQLite (default) bot platform with a Python **ai-service** for LLM calls. Inbound Telegram traffic enters **`TelegramWebhookController`**, is normalized by **`TelegramChannelAdapter`**, and processed by **`IncomingMessageHandler`** using DB-driven **`JudgeBotConfiguration`**.

## Webhook model

- **Multi-bot:** `POST /api/telegram/webhook/{integrationId}` maps to **`BotIntegrations`** (`ExternalId` = token).
- **Legacy:** `POST /api/telegram/webhook` uses config token or a single unambiguous integration.
- **Registration:** `{App:PublicBaseUrl}/api/telegram/webhook/{id}` via Admin **sync-webhook** → Telegram **`setWebhook`**.

## Admin surface

- **HTTP:** `POST/GET/DELETE` under **`/admin/integrations/{id}/...`** with **`X-Admin-Key`**.
- **UI:** `admin-ui` Integrations page (Telegram rows: URL, badges, sync/delete/refresh).

## Security highlights

- Production **HTTPS** enforced for **`App:PublicBaseUrl`** when set.
- **Admin** routes gated by **`Admin:ApiKey`**; Telegram ingress path is public by necessity.
- **Token masking** in logs (`bot_id:***`).

## Operations

Configuration, Docker, ngrok, and go-live steps: **`docs/DEPLOYMENT.md`**, **`docs/CONFIGURATION.md`**, **`docs/OPERATIONAL_CHECKLIST.md`**. Deep Telegram design: **`docs/TELEGRAM_WEBHOOKS.md`**.
