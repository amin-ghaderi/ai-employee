# .NET 10 is in preview until November 2026.
# If "10.0" tag is unavailable, change to "10.0-preview".
#
# Production: multi-stage publish + aspnet runtime (no dotnet watch — avoids
# PollingDirectoryWatcher duplicate-key crashes with Docker bind mounts).
# Development with live mount: use compose profile "api-dev" (SDK + dotnet run).
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY AiEmployee.sln ./
COPY src/AiEmployee.Api/*.csproj               src/AiEmployee.Api/
COPY src/AiEmployee.Application/*.csproj       src/AiEmployee.Application/
COPY src/AiEmployee.Domain/*.csproj            src/AiEmployee.Domain/
COPY src/AiEmployee.Infrastructure/*.csproj    src/AiEmployee.Infrastructure/
COPY tests/AiEmployee.UnitTests/*.csproj       tests/AiEmployee.UnitTests/
COPY tests/AiEmployee.IntegrationTests/*.csproj tests/AiEmployee.IntegrationTests/
RUN dotnet restore

COPY . .
RUN dotnet publish src/AiEmployee.Api/AiEmployee.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5155
ENV ASPNETCORE_URLS=http://+:5155
ENTRYPOINT ["dotnet", "AiEmployee.Api.dll"]
