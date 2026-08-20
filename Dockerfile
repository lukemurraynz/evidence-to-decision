# syntax=docker/dockerfile:1.26@sha256:ecfaec9ed6d810b56388c508f4121597bfbba70d41a6dfeee4d8cad5f295fc32
FROM mcr.microsoft.com/dotnet/sdk:11.0-preview@sha256:e83dfc887721c3a268c964ce651617bcf9d05851e5eea7ad250949170c0fd421 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props OpportunityEngineering.slnx .editorconfig ./
COPY src/OpportunityEngineering.Domain/OpportunityEngineering.Domain.csproj src/OpportunityEngineering.Domain/
COPY src/OpportunityEngineering.Application/OpportunityEngineering.Application.csproj src/OpportunityEngineering.Application/
COPY src/OpportunityEngineering.Infrastructure/OpportunityEngineering.Infrastructure.csproj src/OpportunityEngineering.Infrastructure/
COPY src/OpportunityEngineering.Api/OpportunityEngineering.Api.csproj src/OpportunityEngineering.Api/
COPY src/OpportunityEngineering.Domain/packages.lock.json src/OpportunityEngineering.Domain/
COPY src/OpportunityEngineering.Application/packages.lock.json src/OpportunityEngineering.Application/
COPY src/OpportunityEngineering.Infrastructure/packages.lock.json src/OpportunityEngineering.Infrastructure/
COPY src/OpportunityEngineering.Api/packages.lock.json src/OpportunityEngineering.Api/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore src/OpportunityEngineering.Api/OpportunityEngineering.Api.csproj --locked-mode

COPY src/ src/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish src/OpportunityEngineering.Api/OpportunityEngineering.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:11.0-preview@sha256:385243b43a2e8b30ffde1eab09a3457b7226bc3cbefedbc3a8b06fa35842c7f6 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080
USER $APP_UID
COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .
ENTRYPOINT ["dotnet", "OpportunityEngineering.Api.dll"]
