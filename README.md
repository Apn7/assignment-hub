# Assignment Hub

Assignment & Submission Management System for a school/college, with Admin,
Teacher and Student roles.

> **Status: scaffold.** Project structure, tooling and configuration only — no
> business logic or features are implemented yet.

## Overview

_TODO: what the system does and who it is for._

## Features

_TODO: per-role feature list (Admin / Teacher / Student)._

## Tech Stack

| Layer    | Choice                                                                  |
| -------- | ----------------------------------------------------------------------- |
| Frontend | Next.js 16 (App Router), React 19, TypeScript, Tailwind CSS 4            |
| Forms    | react-hook-form + zod (`@hookform/resolvers`)                            |
| Data     | TanStack Query, axios                                                    |
| Backend  | ASP.NET Core 8 Web API, C#, FluentValidation, Swashbuckle (Swagger)      |
| Database | PostgreSQL 16 via EF Core (Npgsql)                                       |
| Auth     | JWT bearer tokens, role-based authorization                              |
| Testing  | xUnit, Moq, FluentAssertions, `WebApplicationFactory`, EF Core InMemory  |

## Project Structure

```
assignment-hub/
├── backend/
│   ├── AssignmentHub.sln
│   ├── src/
│   │   ├── AssignmentHub.Api/            # Controllers, middleware, composition root
│   │   ├── AssignmentHub.Application/    # Services, DTOs, interfaces, validators
│   │   ├── AssignmentHub.Domain/         # Entities and enums (no dependencies)
│   │   └── AssignmentHub.Infrastructure/ # EF Core, DbContext, repositories
│   └── tests/
│       └── AssignmentHub.Tests/          # Unit + integration tests
├── frontend/                             # Next.js App Router application
│   └── src/
│       ├── app/                          # Routes, layout, providers
│       ├── components/                   # Shared presentational components
│       ├── features/                     # Feature-scoped modules
│       ├── lib/api/                      # axios instance and API calls
│       └── types/                        # Shared TypeScript contracts
├── docs/
├── docker-compose.yml                    # PostgreSQL 16 for local development
└── .env.example
```

Dependencies point inwards: `Api → Application + Infrastructure`,
`Infrastructure → Application + Domain`, `Application → Domain`, and `Domain`
depends on nothing.

## Prerequisites

- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20.9+](https://nodejs.org) (Next.js 16 minimum) and npm
- [Docker](https://docs.docker.com/get-docker/) — or a local PostgreSQL 16 instance

## Database Setup

Start PostgreSQL and wait until it accepts connections:

```bash
docker compose up -d --wait db
```

This creates a database, user and password all named `assignmenthub` on port
`5432`, matching the connection string in
`backend/src/AssignmentHub.Api/appsettings.Development.json`.

Apply migrations (no migrations exist yet — this becomes relevant once entities
are added):

```bash
cd backend
dotnet ef database update \
  --project src/AssignmentHub.Infrastructure \
  --startup-project src/AssignmentHub.Api
```

To stop the database and delete its data: `docker compose down -v`.

## Running the Backend

The JWT signing key is deliberately absent from every committed file, so set it
once before the first run:

```bash
cd backend/src/AssignmentHub.Api
dotnet user-secrets set "Jwt:Secret" "$(openssl rand -base64 48)"
```

Then:

```bash
dotnet run
```

- API: <http://localhost:5080>
- Swagger UI: <http://localhost:5080/swagger>
- Health check: <http://localhost:5080/api/health>

Any setting can be overridden by an environment variable using `__` as the
section separator (`Jwt__Secret`, `ConnectionStrings__Default`). See
`.env.example`.

## Running the Frontend

```bash
cd frontend
cp .env.local.example .env.local   # sets NEXT_PUBLIC_API_URL
npm install
npm run dev
```

Open <http://localhost:3000>. The placeholder page calls `/api/health` and
displays the result.

## Running Tests

```bash
cd backend
dotnet test
```

## Demo Credentials

_TODO: seeded Admin / Teacher / Student logins._

| Role    | Email  | Password |
| ------- | ------ | -------- |
| Admin   | _TODO_ | _TODO_   |
| Teacher | _TODO_ | _TODO_   |
| Student | _TODO_ | _TODO_   |

## API Documentation

Swagger UI is served at `/swagger` in the Development environment; the OpenAPI
document is at `/swagger/v1/swagger.json`.

## Assumptions

_TODO: decisions made where the requirements were not explicit._

## Known Limitations

_TODO: what is intentionally out of scope._
