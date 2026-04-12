# Example configuration files

These files are **templates only**. Copy values into your real **`appsettings.*.json`**, **`.env`**, or **`docker-compose.override.yml`** outside version control when they contain secrets.

| File | Use case |
|------|----------|
| `appsettings.Development.example.json` | Local / ngrok development |
| `appsettings.Production.example.json` | Production-oriented layout (HTTPS URL, internal AI URL) |
| `docker-compose.env.example` | Environment variables for `docker compose` |
