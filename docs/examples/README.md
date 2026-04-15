# Example configuration files

These files are **templates only**. Copy values into your real **`appsettings.*.json`**, **`.env`**, or **`docker-compose.override.yml`** outside version control when they contain secrets.

| File | Use case |
|------|----------|
| `appsettings.Development.example.json` | Local / ngrok development |
| `appsettings.Production.example.json` | Production-oriented layout: **PostgreSQL** connection string (pgvector-capable host), HTTPS URL, internal AI URL. See `docs/PHASE7_POSTGRES_ONLY.md`. |
| `docker-compose.env.example` | Environment variables for `docker compose` |
