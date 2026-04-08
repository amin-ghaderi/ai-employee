# .NET 10 is in preview until November 2026.
# If "10.0" tag is unavailable, change to "10.0-preview".
FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /app

# Copy project files first for NuGet restore caching.
# When using volume mounts (dev mode), the restore runs at container start instead.
COPY AiEmployee.sln ./
COPY src/AiEmployee.Api/*.csproj               src/AiEmployee.Api/
COPY src/AiEmployee.Application/*.csproj       src/AiEmployee.Application/
COPY src/AiEmployee.Domain/*.csproj            src/AiEmployee.Domain/
COPY src/AiEmployee.Infrastructure/*.csproj    src/AiEmployee.Infrastructure/
COPY tests/AiEmployee.UnitTests/*.csproj       tests/AiEmployee.UnitTests/
COPY tests/AiEmployee.IntegrationTests/*.csproj tests/AiEmployee.IntegrationTests/
RUN dotnet restore

COPY . .

EXPOSE 5155

ENTRYPOINT ["dotnet", "watch", "run", "--project", "src/AiEmployee.Api", "--urls", "http://0.0.0.0:5155"]
