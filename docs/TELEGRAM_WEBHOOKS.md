# Telegram Webhook Management

This document describes how Telegram webhooks work in AI Employee, how to manage them from the Admin API and UI, and how multi-bot routing behaves.

## 1. Dynamic webhook architecture

Telegram delivers updates to a **single HTTPS URL per bot token**. This platform supports **one webhook URL per `BotIntegration`** (Telegram channel row), where the URL embeds the integration primary key:

```text
{App:PublicBaseUrl}/api/telegram/webhook/{integrationId}
```

- **`App:PublicBaseUrl`** — Public origin of the API (no trailing slash), e.g. `https://api.example.com` or an ngrok URL. Used when registering the webhook with Telegram via **`setWebhook`**.
- **`integrationId`** — `BotIntegrations.Id` (GUID). Telegram’s POST body is unchanged; the server uses the path segment to load the correct **`BotIntegration`** and token (**`ExternalId`**).

**Flow:**

1. Operator sets **`App:PublicBaseUrl`** and ensures the URL is reachable over **HTTPS** (required by Telegram in real deployments; Development may use HTTP for local testing—see [CONFIGURATION.md](./CONFIGURATION.md)).
2. Operator calls **Sync Webhook** (Admin API or admin-ui). The backend calls Telegram **`setWebhook`** with the computed URL.
3. Telegram sends **`Update`** JSON to that URL. **`TelegramWebhookController`** forwards to **`TelegramChannelAdapter`**, which resolves **`ExternalId`** (bot token) from the integration row (or legacy paths—see below).

**Legacy single-bot URL** (still supported):

```text
{App:PublicBaseUrl}/api/telegram/webhook
```

Resolution then uses **`Telegram:BotToken`**, or a single enabled Telegram integration, or fails if ambiguous—see `TelegramChannelAdapter`.

## 2. Multi-bot support

- Each **Telegram bot** should have its own **`BotIntegration`** row: `Channel = "telegram"`, **`ExternalId`** = that bot’s token (`{id}:{secret}`).
- Each bot’s Telegram webhook should point to **`.../webhook/{that_row’s_Id}`**.
- Inbound messages carry **`IncomingMessageMetadataKeys.IntegrationExternalId`** so **`IBotResolver`** can load the correct **`JudgeBotConfiguration`**.

**Rules enforced in `TelegramChannelAdapter` for the integration route:**

- Integration must exist, be **enabled**, channel must be **telegram**, and **`ExternalId`** must be non-empty.

## 3. Admin API endpoints

All routes are under **`/admin`** and require header **`X-Admin-Key`** matching configuration **`Admin:ApiKey`** (see [CONFIGURATION.md](./CONFIGURATION.md)).

| Method | Path | Purpose |
|--------|------|---------|
| `POST` | `/admin/integrations/{id}/sync-webhook` | Calls Telegram **`setWebhook`** for that integration’s token. |
| `GET` | `/admin/integrations/{id}/webhook-status` | Calls Telegram **`getWebhookInfo`**; compares URL to expected URL when `PublicBaseUrl` is set. |
| `DELETE` | `/admin/integrations/{id}/webhook` | Calls Telegram **`deleteWebhook`**. Optional query: `dropPendingUpdates=true`. |

**Response shape** (JSON, camelCase): `webhookUrl`, `status`, `lastError`, `lastSyncedAt`.

**Typical `status` values:** `synced`, `active`, `not_registered`, `deleted`, `error`, `mismatch`, `not_found`.

**HTTP status:** `401` without valid admin key; `404` for unknown integration; `400` for configuration/client issues (e.g. invalid `PublicBaseUrl` in Production); `502` for Telegram/network failures (body still contains the DTO when possible).

## 4. Frontend usage (admin-ui)

- Configure **`VITE_API_URL`** to the API origin **without** `/admin` (e.g. `http://localhost:5155`). The client appends **`/admin`** and sends **`X-Admin-Key`** from **`VITE_ADMIN_KEY`** (see `admin-ui/src/api/client.js`).
- **Integrations** page: for rows with `channel === "telegram"`, the UI shows a read-only **Webhook URL**, **status badge**, **Sync Webhook**, **Refresh status**, and **Delete Webhook**.

## 5. Telegram API responses (reference)

Success examples:

```json
{"ok":true,"result":true,"description":"Webhook was set"}
```

```json
{"ok":true,"result":{"url":"https://...","pending_update_count":0}}
```

```json
{"ok":true,"result":true}
```

Telegram may return HTTP 200 with **`"ok": false`** for logical errors; the backend maps these to failures and logs masked token context.

## 6. Security best practices

| Topic | Implementation / guidance |
|--------|----------------------------|
| **HTTPS (Production)** | `PublicBaseUrlProvider` throws if **`App:PublicBaseUrl`** is not **HTTPS** when `ASPNETCORE_ENVIRONMENT=Production`. |
| **Token masking** | Logs use **`TelegramTokenMasking`** / **`TelegramTokenUtilities.MaskBotToken`** (`bot_id:***`). Never log full tokens. |
| **Admin API** | `AdminAuthMiddleware` requires **`X-Admin-Key`** for paths starting with **`/admin`**. Use a long random secret in production. |
| **Webhook validation** | Telegram updates are accepted on **`/api/telegram/...`** without the admin key (required by Telegram). Mitigations: HTTPS only in production, unguessable **`integrationId`**, optional IP allowlisting at the edge, and (future) Telegram **`secret_token`** on `setWebhook` + middleware validation—not implemented in this codebase yet; treat as hardening backlog. |

## 7. Logging

Look for log categories:

- **`TelegramWebhookController`** — inbound update id, dispatch, errors (returns 200 to Telegram on handler errors to reduce retry storms—by design).
- **`TelegramWebhookApiClient`** — outbound Telegram API calls, masked token, response preview.
- **`TelegramWebhookApplicationService`** — sync/info/delete orchestration.

---

See also: [DEPLOYMENT.md](./DEPLOYMENT.md), [CONFIGURATION.md](./CONFIGURATION.md), [OPERATIONAL_CHECKLIST.md](./OPERATIONAL_CHECKLIST.md).
