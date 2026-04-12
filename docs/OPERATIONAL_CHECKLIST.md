# Operational checklist (Telegram & production)

Use this before go-live or after material changes.

## Configuration

- [ ] **`ASPNETCORE_ENVIRONMENT`** set appropriately (`Production` in prod).
- [ ] **`App:PublicBaseUrl`** set to **HTTPS** origin (no trailing slash); matches public URL users/Telegram hit.
- [ ] **`Ai:BaseUrl`** points to a healthy **ai-service** instance.
- [ ] **`Admin:ApiKey`** set to a strong secret; not default `your-secret-key` in production.
- [ ] **`ConnectionStrings:DefaultConnection`** valid and backed up if SQLite file is used.

## Telegram

- [ ] Each bot has a **`BotIntegration`** with `channel = telegram` and token in **`ExternalId`**.
- [ ] **Sync Webhook** executed for each integration; **`webhook-status`** shows **`active`** (or acceptable state).
- [ ] Legacy **`Telegram:BotToken`** only used if intentionally; no conflicting ambiguous multi-integration setup for legacy `/api/telegram/webhook` route.
- [ ] BotFather / Telegram side: webhook URL matches **`{PublicBaseUrl}/api/telegram/webhook/{id}`**.

## Security

- [ ] Admin routes return **401** without **`X-Admin-Key`** (spot-check with `curl`).
- [ ] **`/api/telegram`** is **not** behind admin key (Telegram requirement); protected by HTTPS + URL design at edge.
- [ ] Logs reviewed to ensure **no full bot tokens** appear (masked `id:***` only).

## Frontend (if used)

- [ ] **`VITE_API_URL`** matches API host (scheme + host + port).
- [ ] **`VITE_ADMIN_KEY`** matches **`Admin:ApiKey`**.

## Post-deploy smoke

- [ ] Send a test message to the bot; confirm **`Telegram webhook: completed`** in logs.
- [ ] Judge / reply path works end-to-end (depends on persona, AI service, etc.).

## Rollback

- [ ] Document previous **`App:PublicBaseUrl`** and webhook URLs if reverting.
- [ ] **`deleteWebhook`** (admin) if you must detach Telegram quickly from this host.
