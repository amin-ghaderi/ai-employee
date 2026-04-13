# Example configuration files

These files are **templates only**. Copy values into your real **`appsettings.*.json`**, **`.env`**, or **`docker-compose.override.yml`** outside version control when they contain secrets.

| File | Use case |
|------|----------|
| `appsettings.Development.example.json` | Local / ngrok development |
| `appsettings.Production.example.json` | Production-oriented layout: **PostgreSQL** (`Database:Provider` + Npgsql connection string), HTTPS URL, internal AI URL. **SQLite is not used** in this template (deprecated for production; see Phase 5 cutover doc). |
| `docker-compose.env.example` | Environment variables for `docker compose` |
