# syntax=docker/dockerfile:1

# Build context is `backend/`, not the Api project directory: the Api project has
# ProjectReferences to Domain, Application and Infrastructure, so all four have to be
# inside the context for `dotnet restore` to resolve them.

# ---------------------------------------------------------------------------
# Build
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Project files first, on their own layer. Restore is the slow step and depends only
# on these, so editing a .cs file no longer invalidates it.
COPY src/AssignmentHub.Domain/AssignmentHub.Domain.csproj                 src/AssignmentHub.Domain/
COPY src/AssignmentHub.Application/AssignmentHub.Application.csproj       src/AssignmentHub.Application/
COPY src/AssignmentHub.Infrastructure/AssignmentHub.Infrastructure.csproj src/AssignmentHub.Infrastructure/
COPY src/AssignmentHub.Api/AssignmentHub.Api.csproj                       src/AssignmentHub.Api/

RUN dotnet restore src/AssignmentHub.Api/AssignmentHub.Api.csproj

COPY . .

RUN dotnet publish src/AssignmentHub.Api/AssignmentHub.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---------------------------------------------------------------------------
# Runtime
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

# Kestrel must bind every interface, not just loopback, or the platform's health
# probe and ingress cannot reach it. 8080 matches the container app's targetPort.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Everything else — ASPNETCORE_ENVIRONMENT, Jwt__Secret, ConnectionStrings__Default,
# Cors__AllowedOrigins__0 — is supplied by the platform as environment variables.
# Nothing configurable is baked into this image.
ENTRYPOINT ["dotnet", "AssignmentHub.Api.dll"]
