# Phase 7 — PostgreSQL only

## Summary

- **SQLite is removed.** The API and EF Core use **one** `AiEmployeeDbContext` backed by **Npgsql** with the **`vector`** extension (pgvector).
- **Migration history** remains in **`public.__EFMigrationsHistory_Postgres`** so existing PostgreSQL deployments keep a continuous EF history without replaying legacy SQLite migrations.
- **Legacy tooling:** the one-off `AiEmployee.DataMigrator` (SQLite → Postgres) has been removed from the solution; new environments should start from PostgreSQL migrations only.
- **Integration tests** use **Testcontainers** with the **`pgvector/pgvector:pg16`** image so CI and local runs exercise the same database engine as production.

## Configuration

- Set **`ConnectionStrings:DefaultConnection`** to a PostgreSQL connection string everywhere (local `appsettings.*.json`, Docker, Kubernetes secrets).
- **`docker-compose.yml`** no longer sets `Database__Provider`; only the connection string matters.

## EF Core CLI

From the repository root (design-time factory reads `POSTGRES_DESIGN_CONNECTION` or `ConnectionStrings__DefaultConnection`):

```bash
dotnet ef migrations add YourMigrationName ^
  --project src/AiEmployee.Infrastructure/AiEmployee.Infrastructure.csproj ^
  --startup-project src/AiEmployee.Api/AiEmployee.Api.csproj ^
  --context AiEmployeeDbContext ^
  --output-dir Persistence/Migrations/PostgreSql
```

## OpenTelemetry (production)

When **`OTEL_EXPORTER_OTLP_ENDPOINT`** or **`OpenTelemetry:Otlp:Endpoint`** is set to an absolute URL (for example `http://localhost:4317` for gRPC OTLP), the API registers:

- **Tracing:** ASP.NET Core instrumentation + `ActivitySource` **`AiEmployee.AI`** (spans from `AiClient`, e.g. `ai.chat`).
- **Metrics:** ASP.NET Core instrumentation + **`Meter`** **`AiEmployee.AI`** (schema validation counters and latency histograms from Phase 6).

If the endpoint is unset, OpenTelemetry services are not registered (no overhead).

## Operational notes

- Run a **pgvector** image or extension-compatible managed Postgres (same as `docker-compose.yml`: `pgvector/pgvector:pg16`).
- Integration tests require **Docker** (Testcontainers).
