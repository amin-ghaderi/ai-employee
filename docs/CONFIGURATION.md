# Configuration reference

ASP.NET Core binds hierarchical configuration. Use **`__`** (double underscore) in environment variables, or nested JSON in `appsettings*.json`.

## Required / common variables

| Key (env / JSON) | Description |
|------------------|-------------|
| **`App__PublicBaseUrl`** / `App:PublicBaseUrl` | Public base URL of this API (**no trailing slash**). Used to build Telegram webhook URLs and admin “expected webhook” comparisons. Empty = optional for non-Telegram features; required for **Sync Webhook** to succeed. **HTTPS required in Production** when set. |
| **`Telegram__BotToken`** / `Telegram:BotToken` | Optional legacy default bot token. **Preferred:** store each bot token in **`BotIntegrations.ExternalId`** for `channel = telegram`. |
| **`Ai__BaseUrl`** / `Ai:BaseUrl` | Base URL of the Python **ai-service** (e.g. `http://localhost:8000` or `http://ai-service:8000` in Docker). |
| **`Admin__ApiKey`** / `Admin:ApiKey` | Shared secret for **`X-Admin-Key`** on **`/admin/**`** routes. Must be non-empty for admin access. |
| **`ConnectionStrings__DefaultConnection`** | **PostgreSQL (required):** e.g. `Host=postgres;Port=5432;Database=aiemployee;Username=…;Password=…`. Use a **pgvector**-capable server (see `docker-compose.yml`). |
| **`OTEL_EXPORTER_OTLP_ENDPOINT`** / `OpenTelemetry:Otlp:Endpoint` | Optional. When set to an absolute OTLP URL, exports **traces** and **metrics** (including `AiEmployee.AI`). See `docs/PHASE7_POSTGRES_ONLY.md`. |

## Environment-specific notes

- **Development:** `PublicBaseUrl` may be `http://127.0.0.1:5155` or an **https** ngrok URL. HTTP is allowed only when the host environment is **not** Production.
- **Production:** Set **`App__PublicBaseUrl`** to **`https://your-domain`** only. HTTP values throw at runtime when resolving the URL.

## Docker

In `docker-compose.yml`, the **`api`** service passes variables with `__` nesting, e.g.:

```yaml
environment:
  - App__PublicBaseUrl=https://your-host.example
  - Ai__BaseUrl=http://ai-service:8000
  - Admin__ApiKey=${ADMIN_API_KEY}
```

## Example files

Copy and adapt:

- [`examples/appsettings.Development.example.json`](./examples/appsettings.Development.example.json)
- [`examples/appsettings.Production.example.json`](./examples/appsettings.Production.example.json)

Do **not** commit real bot tokens or production admin keys into the repository.
