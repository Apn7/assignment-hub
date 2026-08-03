# Database Setup

PostgreSQL 16 via EF Core (Npgsql). Migrations create the schema; a
Development-only seeder loads demo data. The evaluator never has to create a
table by hand.

## Prerequisites

- Docker (for the bundled Postgres), or a local PostgreSQL 16 instance
- .NET SDK 8.0
- The `dotnet-ef` CLI, **pinned to 8.x** — the current release targets `net9.0`+
  and will not run on an 8-only SDK:

  ```bash
  dotnet tool install --global dotnet-ef --version "8.0.*"
  ```

## 1. Start PostgreSQL

From the repository root:

```bash
docker compose up -d --wait db
```

`--wait` blocks until the healthcheck passes, so the migration below cannot run
against a database that is still starting up.

This creates database, user and password all named `assignmenthub` on port
`5432`, matching the connection string in
`backend/src/AssignmentHub.Api/appsettings.Development.json`.

## 2. Set the JWT signing key (once)

The API refuses to start without it, and `dotnet ef` boots the API host, so this
is required before migrating:

```bash
cd backend/src/AssignmentHub.Api
dotnet user-secrets set "Jwt:Secret" "$(openssl rand -base64 48)"
```

Verify with `dotnet user-secrets list` — the key must read exactly `Jwt:Secret`,
with no surrounding quote characters.

## 3. Apply migrations

`dotnet ef` does **not** read `launchSettings.json`, so the environment has to be
set explicitly or user-secrets will not load.

```bash
# bash / zsh
cd backend
ASPNETCORE_ENVIRONMENT=Development dotnet ef database update \
  --project src/AssignmentHub.Infrastructure \
  --startup-project src/AssignmentHub.Api
```

```powershell
# PowerShell
cd backend
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update `
  --project src/AssignmentHub.Infrastructure `
  --startup-project src/AssignmentHub.Api
```

## 4. Seed demo data

Start the API; the seeder runs automatically in Development:

```bash
cd backend/src/AssignmentHub.Api
dotnet run --launch-profile http
```

Look for `Seed complete: 7 users, 2 classes, 3 subjects, 4 teacher assignments,
3 assignments, 2 submissions.` in the log.

The seeder is **idempotent** — it skips entirely if any user already exists, so
restarting the API never duplicates data. It also skips with a warning if
migrations are still pending.

## Demo credentials

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@assignmenthub.local` | `Admin#1234` |
| Teacher | `teacher1@assignmenthub.local` | `Teacher#1234` |
| Teacher | `teacher2@assignmenthub.local` | `Teacher#1234` |
| Student | `student1@assignmenthub.local` | `Student#1234` |
| Student | `student2@assignmenthub.local` | `Student#1234` |
| Student | `student3@assignmenthub.local` | `Student#1234` |
| Student | `student4@assignmenthub.local` | `Student#1234` |

Passwords are hashed with `PasswordHasher<User>` (Identity.Core, PBKDF2). No
plaintext password is ever stored. These are local fixtures and are committed
deliberately so the project can be evaluated; they are only ever written to a
Development database.

## What gets seeded

| Entity | Data |
| --- | --- |
| Classes | `Class 9 – A`, `Class 10 – A` |
| Subjects | Physics, Mathematics, English |
| Teacher assignments | teacher1 → Class 9 – A (Physics, Mathematics); teacher2 → Class 10 – A (English), Class 9 – A (English) |
| Students | student1, student2 → Class 9 – A; student3, student4 → Class 10 – A |
| Assignments | all by teacher1 for Class 9 – A (see below) |
| Submissions | 2 on *Kinematics Problem Set*, from student1 and student2 |

The three assignments deliberately cover every state the UI must render:

| Title | Subject | Status | Deadline | Max marks |
| --- | --- | --- | --- | --- |
| Newton's Laws of Motion – Worksheet 1 | Physics | `Draft` | +14 days | 20 |
| Kinematics Problem Set | Physics | `Published` | +7 days (open) | 50 |
| Quadratic Equations Revision | Mathematics | `Published` | −3 days (closed) | 30 |

Of the two submissions, student1's is `Submitted` (no marks) and student2's is
`Reviewed` with 42/50 and feedback — so both the ungraded and graded paths have
data.

Entity ids are fixed GUIDs rather than random, so a re-seeded database always
looks identical and manual API calls stay reproducible.

## Schema notes

- **Enums as strings.** `UserRole`, `AssignmentStatus` and `SubmissionStatus` are
  stored via `HasConversion<string>()` as `character varying(20)`, so the
  database is readable and adding a member never renumbers existing rows.
- **All timestamps are `timestamptz` in UTC.** A global `UtcDateTimeConverter`
  applied in `ConfigureConventions` normalises every `DateTime` and `DateTime?`,
  because Npgsql throws when handed a non-UTC `DateTimeKind`.
- **No cascade deletes.** All 9 foreign keys use `DeleteBehavior.Restrict`;
  removing a referenced class, subject or user fails loudly rather than silently
  destroying assignment history.
- **Unique constraints**, all enforced by the database rather than only in
  application code:
  - `Users.Email`
  - `TeacherAssignments (TeacherId, ClassRoomId, SubjectId)`
  - `Submissions (AssignmentId, StudentId)` — one submission per student per
    assignment, safe against concurrent requests.
- **`ClassRoom`, not `Class`** — `class` is a C# keyword and would force
  `@class` at every usage site.
- Entity shape lives in `Infrastructure/Persistence/Configurations` as
  `IEntityTypeConfiguration<T>` classes. The Domain entities carry no
  persistence annotations.

## Resetting

```bash
docker compose down -v          # stops the container and deletes the volume
docker compose up -d --wait db
# then repeat step 3 (migrate) and step 4 (seed)
```

## Useful queries

```bash
docker compose exec db psql -U assignmenthub -d assignmenthub

\dt                                     -- list tables
\d "Submissions"                        -- inspect one table
select "Email", "Role" from "Users";    -- note: quoted, identifiers are PascalCase
```
