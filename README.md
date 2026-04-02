# AiEmployee

Hybrid system with:
- C# ASP.NET Core Web API (`src/AiEmployee.Api`) as the core system
- Python FastAPI (`ai-service`) as an external AI service

## Architecture

- `AiEmployee.Api` -> `AiEmployee.Application`
- `AiEmployee.Application` -> `AiEmployee.Domain`
- `AiEmployee.Infrastructure` -> `AiEmployee.Domain`
- `AiEmployee.Domain` has no project dependencies

AI logic is consumed via `IAiClient` in the domain and implemented in infrastructure.

## Run

1. Start AI service:
   - `cd ai-service`
   - `pip install -r requirements.txt`
   - `uvicorn main:app --reload`

2. Start .NET API (new terminal):
   - `dotnet run --project src/AiEmployee.Api`

3. Test webhook:
   - `POST http://localhost:5119/telegram/webhook` (or the printed API port)
   - Body:
     ```json
     {
       "userId": "123",
       "text": "Please review my shift request"
     }
     ```
