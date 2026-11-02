# ==============================================================================
# Stage 1: Build & Publish (Cantina.Api + Site Web Fura-Fila em wwwroot/)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src/Cantina.Domain/Cantina.Domain.csproj src/Cantina.Domain/
COPY src/Cantina.Contracts/Cantina.Contracts.csproj src/Cantina.Contracts/
COPY src/Cantina.Application/Cantina.Application.csproj src/Cantina.Application/
COPY src/Cantina.Infrastructure/Cantina.Infrastructure.csproj src/Cantina.Infrastructure/
COPY src/Cantina.Api/Cantina.Api.csproj src/Cantina.Api/

RUN dotnet restore src/Cantina.Api/Cantina.Api.csproj

COPY src/Cantina.Domain/ src/Cantina.Domain/
COPY src/Cantina.Contracts/ src/Cantina.Contracts/
COPY src/Cantina.Application/ src/Cantina.Application/
COPY src/Cantina.Infrastructure/ src/Cantina.Infrastructure/
COPY src/Cantina.Api/ src/Cantina.Api/

RUN dotnet publish src/Cantina.Api/Cantina.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ==============================================================================
# Stage 2: Runtime leve para nuvem gratuita (Render, Fly.io, Railway, Koyeb)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://0.0.0.0:8080 \
    TZ=America/Sao_Paulo

RUN mkdir -p /app/data && chown -R $APP_UID:$APP_UID /app
COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .

USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "Cantina.Api.dll"]
