# syntax=docker/dockerfile:1

# Véritas API — multi-stage build.
#
#   docker build -t veritas-api .
#
# Stage 1 (build) has the full .NET SDK (~800 MB) and compiles the code.
# Stage 2 (runtime) starts again from the much smaller ASP.NET runtime image
# and receives only the published output: no SDK, no source code, no
# test project in what actually runs in production.

# ---------- Stage 1: build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project files first and restore. Docker caches each step:
# as long as no .csproj changes, the (slow) restore layer is reused and a
# code-only change rebuilds in seconds.
COPY LuxuryStore.Domain/LuxuryStore.Domain.csproj LuxuryStore.Domain/
COPY LuxuryStore.Application/LuxuryStore.Application.csproj LuxuryStore.Application/
COPY LuxuryStore.Infrastructure/LuxuryStore.Infrastructure.csproj LuxuryStore.Infrastructure/
COPY LuxuryStore-Backend/LuxuryStore-Backend.csproj LuxuryStore-Backend/
RUN dotnet restore LuxuryStore-Backend/LuxuryStore-Backend.csproj

# Now the source code (filtered by .dockerignore), then compile and publish.
COPY . .
RUN dotnet publish LuxuryStore-Backend/LuxuryStore-Backend.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    -p:UseAppHost=false

# ---------- Stage 2: runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# The .NET images ship a non-root user ("app"). Running as it means a
# compromised process can't modify the image's files or the system.
USER $APP_UID

# Kestrel listens on plain HTTP inside the container. In production TLS is
# terminated in front of it by Render; tell Render the port with the
# environment variable PORT=8080.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Extra arguments are passed through to the app, so the same image also
# runs the one-shot modes:  docker run ... veritas-api --migrate
ENTRYPOINT ["dotnet", "LuxuryStore-Backend.dll"]
