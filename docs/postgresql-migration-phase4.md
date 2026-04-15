# PostgreSQL migration — Phase 4 runbook (archived)

> **Phase 7:** One-off SQLite → PostgreSQL **ETL** (`tools/AiEmployee.DataMigrator`) and dual `DbContext` migrations are **removed**. This file is kept as a **historical** record of Phase 4 operations. For current setup, see **[PHASE7_POSTGRES_ONLY.md](./PHASE7_POSTGRES_ONLY.md)** and **[postgresql-cutover-phase5.md](./postgresql-cutover-phase5.md)** (Phase 7 banner).

## What still applies

- **Docker networking:** Compose may leave Postgres on the internal `backend` network only. To run `dotnet ef` from the host against that Postgres, either attach an SDK container to `aiemployee_backend` (same pattern as before) or publish port `5432` via a gitignored `docker-compose.override.yml`.
- **Schema:** Apply and evolve the database with **`AiEmployeeDbContext`** and Npgsql; migration history table **`public.__EFMigrationsHistory_Postgres`**.

## What no longer applies

- **`SqliteToPostgresMigrator`**, dry-run / `--truncate` CLI flows, and **`Database__Provider`** switching.
- Dockerfile **`COPY tools/AiEmployee.DataMigrator`** (removed from the image build).

## EF Core from Docker (current)

Use the same `Host=postgres;...` connection string as the API service, then:

```bash
dotnet ef database update \
  --project src/AiEmployee.Infrastructure/AiEmployee.Infrastructure.csproj \
  --startup-project src/AiEmployee.Api/AiEmployee.Api.csproj \
  --context AiEmployeeDbContext
```

## Related

- [CONFIGURATION.md](./CONFIGURATION.md)  
- [DEPLOYMENT.md](./DEPLOYMENT.md)  
