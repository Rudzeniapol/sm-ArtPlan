FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore as a separate layer so package downloads are cached across code changes.
COPY GoalsBot/GoalsBot.csproj GoalsBot/
RUN dotnet restore GoalsBot/GoalsBot.csproj

COPY GoalsBot/ GoalsBot/
RUN dotnet publish GoalsBot/GoalsBot.csproj \
        -c Release \
        -o /app \
        /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app

# The MS .NET runtime image already ships a non-root `app` user; just switch to it.
COPY --from=build --chown=app:app /app ./
USER app

ENV DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0

ENTRYPOINT ["dotnet", "GoalsBot.dll"]
