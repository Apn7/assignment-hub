# How this codebase works

A plain-English tour of Assignment Hub: what the pieces are, why they are arranged
this way, and what happens when a request comes in. Written for someone new to
ASP.NET Core.

The other docs are reference material for each feature
([auth](auth.md), [assignments](assignments.md), [submissions](submissions.md),
[database](database.md)). This one is the map.

---

## 1. What we are building

A school app with three kinds of users:

- **Teacher** — writes assignments, publishes them to a class, marks the answers.
- **Student** — sees the assignments for their own class, hands in an answer, sees
  their mark and feedback.
- **Admin** — manages people and classes, and can see everything.

The whole loop, end to end:

```
teacher writes a draft
   ↓ publishes it
students in that class can now see it
   ↓ one of them submits an answer
teacher sees the answer in a marking list
   ↓ gives 8 out of 10 with a comment
student sees "Reviewed — 8/10 — good work, check Q4"
   ↓ teacher can reopen it if a revision is warranted
student revises, teacher re-marks
```

That loop is **built and working today**. What is not built yet is the frontend and
the admin's user/class management screens (see §11).

---

## 2. The tech, and why

| Piece | What it is | Why |
| --- | --- | --- |
| **ASP.NET Core 8** | Microsoft's web framework for C# | Required by the brief |
| **PostgreSQL 16** | The database | Required by the brief (Postgres or Mongo). Relational fits — this data is all rows with relationships |
| **Entity Framework Core** | An ORM: lets us write C# instead of SQL | Standard for .NET. Also gives us *migrations* (see §5) |
| **Docker Compose** | Runs Postgres in a container | So you do not have to install Postgres on your machine |
| **JWT** | A signed token that proves who you are | Required by the brief |
| **xUnit + Moq + FluentAssertions** | Test framework, fake objects, readable assertions | The standard trio in .NET |
| **FluentValidation** | Checks incoming request shapes | Cleaner than scattering `if (x == null)` everywhere |
| **Swagger** | Auto-generated API docs you can click | Required by the brief, and it is how we test by hand |
| **Next.js + TypeScript** | The frontend | Required by the brief. Scaffolded, not built yet |

**ORM in one line:** instead of writing `SELECT * FROM "Assignments" WHERE ...`, you
write `_context.Assignments.Where(a => a.Status == Published)` and EF Core turns
that into SQL for you.

---

## 2b. How the work was sequenced

48 commits in seven deliberate stages. Nothing was built before the thing it depends
on, which is why every commit compiles.

| Stage | What landed | Commits |
| --- | --- | --- |
| 1 | Scaffold only — projects, Docker, config, error contract, Swagger, CORS, logging, health endpoint, test harness, Next.js shell. **No business logic.** | 13 |
| 2 | Extracted the PDF brief into `docs/requirement.md` so there is a source of truth in the repo | 1 |
| 3 | Six entities, EF Core mappings, `InitialSchema` migration, seeder, `docs/database.md` | 6 |
| 4 | Authentication and role authorization only — login, `/me`, JWT, password hashing | 8 |
| 5 | Assignments — teacher CRUD + publish, student read, admin read | 10 |
| 6 | Submissions — submit, revise, grade, reopen, admin view | 10 |
| — | This walkthrough | 1 |

Two habits visible in `git log` that are worth pointing at:

- **Commits are ordered so the app never breaks in between.** In the submissions
  stage, the *repository* landed before the commit that registers the service in
  dependency injection — otherwise there would be a commit that compiles but fails
  to resolve at request time.
- **Refactors are separate commits from features.** `refactor(app): share not-found
  messages so two services cannot drift` does one thing and is reviewable on its
  own.

---

## 3. The four projects, and the one rule that matters

Open `backend/AssignmentHub.sln` and you see four projects:

```
AssignmentHub.Domain          the nouns        (User, Assignment, Submission)
AssignmentHub.Application     the rules        (who may do what, when)
AssignmentHub.Infrastructure  the plumbing     (database, password hashing, tokens)
AssignmentHub.Api             the front door   (HTTP endpoints, Swagger, auth setup)
```

This is called **Clean Architecture**. There are a hundred blog posts about it, but
the whole idea is one rule:

> **Arrows point inward.** Domain depends on nothing. Application depends only on
> Domain. Infrastructure and Api depend on Application.

```
        Api  ─────────┐
                      ├──►  Application  ──►  Domain
        Infrastructure┘
```

**Why bother?** Because the rules — "a student only sees their own class's work" —
are the part that actually matters, and they should not be tangled up with EF Core
or HTTP. The practical payoff, which you can point at:

1. **The rules are testable without a database.** All 127 tests run in about a
   second, because the 103 that cover rules, login and tokens never touch Postgres
   or HTTP at all.
2. **You cannot accidentally cheat.** The Application project does not even
   *reference* the EF Core package, so it is physically impossible to write a
   database query inside a business rule. The compiler enforces the design.

That second point is worth saying out loud, because it is a real, checkable
property, not a style preference. Look at
`backend/src/AssignmentHub.Application/AssignmentHub.Application.csproj` — the only
packages are FluentValidation and a couple of Microsoft *abstractions*. No EF Core,
no ASP.NET Core.

### What goes where

| Folder | Contains | Example |
| --- | --- | --- |
| `Domain/Entities` | Plain C# classes matching database tables | `Assignment.cs` |
| `Domain/Enums` | Fixed sets of values | `AssignmentStatus { Draft, Published }` |
| `Application/Interfaces` | **Promises** — "something can fetch an assignment by id" | `IAssignmentRepository.cs` |
| `Application/Services` | The rules themselves | `AssignmentService.cs` |
| `Application/DTOs` | The shapes that go in and out over HTTP | `AssignmentResponse.cs` |
| `Application/Validators` | Request shape checks | `AssignmentWriteRequestValidator.cs` |
| `Infrastructure/Repositories` | **Keeps** those promises, using EF Core | `AssignmentRepository.cs` |
| `Infrastructure/Persistence` | Table mappings, migrations, the seeder | `AppDbContext.cs` |
| `Api/Controllers` | HTTP endpoints. Deliberately tiny | `AssignmentsController.cs` |

**Interface** (the `I...` files) is the key idea if it is new to you. It is a
contract with no code in it — just a list of methods something must have. The
Application layer says "I need something that can fetch an assignment"; the
Infrastructure layer says "I'm something that can do that, using EF Core." Tests
say "I'm also something that can do that, using a `List<>`." Neither the rule code
nor the test needs to know which is which.

---

## 4. The data model

Six tables. `docs/database.md` has the full detail; here is the shape:

```
ClassRoom ──┬── User (students belong to a class)
            │
Subject ────┼── TeacherAssignment  ← "Ayesha may teach Physics to Class 9-A"
            │
            └── Assignment ── Submission ── User (the student who wrote it)
```

**`TeacherAssignment` is the one to understand.** It is a join table with one row
per *teacher + class + subject* combination, and it is the answer to "may this
teacher set work here?" Having a `Teacher` role only says you are *a* teacher. It
says nothing about *which* classes are yours. That distinction is the difference
between role-based access and resource-based access, and most of the interesting
rules live on the second one.

Two naming things a reviewer might ask about:

- **`ClassRoom`, not `Class`** — `class` is a reserved keyword in C#. Calling it
  `Class` would mean writing `@class` at every single usage.
- **One `Users` table for all three roles**, with a `Role` column, because all three
  share every field except class membership. Three tables would mean three login
  paths.

---

## 5. Migrations and the seeder

Two ideas that go together, both required by the brief ("the evaluator should be
able to set up the database without manually creating tables").

**Migrations** are versioned C# files that build the database schema.
`Migrations/20260803134607_InitialSchema.cs` contains the `CREATE TABLE` statements
as C# calls. You run:

```bash
dotnet ef database update
```

and EF Core applies any migration not yet applied. This is how the evaluator gets a
working database without writing a line of SQL.

**The seeder** (`Persistence/Seed/DataSeeder.cs`) then fills that empty schema with
demo data: 7 users (one admin, two teachers, four students), 2 classes, 3 subjects,
3 assignments and 2 submissions. It runs automatically on startup, but only in
Development.

Two properties of the seeder worth knowing:

- **It is idempotent** — restart the API ten times and you still have 7 users. It
  checks `if (await _context.Users.AnyAsync()) return;` first.
- **It hashes passwords through the same code the login path uses.** It resolves
  `IPasswordHasher`, exactly as `AuthService` does. If it used its own hashing, a
  seeded password might not verify at login — a genuinely annoying bug to chase.

It also refuses to run if migrations are still pending, and logs a warning instead
of half-seeding a schema that does not exist yet.

---

## 6. Trace one request, end to end

This is the single most useful thing to understand. Take:

```
POST /api/assignments/{id}/submissions
Authorization: Bearer eyJhbGc...
{ "answerText": "Q1: 24 m/s ..." }
```

**Step 1 — Middleware.** `Program.cs` sets up a pipeline every request walks
through, in order:

```
exception handler  →  routing  →  CORS  →  authentication  →  authorization  →  controller
```

`UseAuthentication` reads the `Authorization` header, checks the token's signature
against our secret key, and if valid attaches a `User` object holding the token's
claims. If the signature is wrong or the token is expired: **401**, and the request
never reaches our code.

**Step 2 — Authorization.** The action carries
`[Authorize(Roles = nameof(UserRole.Student))]`. The framework compares that to the
`role` claim in the token. A teacher's token here gets **403**.

**Step 3 — Validation.** FluentValidation runs `SubmitAnswerRequestValidator`
against the body. Empty answer, or over 20,000 characters: **400**, with a
field-level error list. The controller body never runs.

**Step 4 — Controller.** Three lines. It reads the caller's id from the token
(`CurrentUserId`), calls the service, and converts the answer to HTTP. That is
genuinely all a controller does in this codebase:

```csharp
var result = await _submissions.SubmitAsync(CurrentUserId, assignmentId, request, ct);
return ToActionResult(result, StatusCodes.Status201Created);
```

**Step 5 — Service.** `SubmissionService.SubmitAsync` is where the thinking happens:

1. Look up the student to find their class. *Not* from the request — from the
   database.
2. Ask for the assignment **as visible to that class**. A draft or another class's
   work comes back as nothing → **404**.
3. Is it past the deadline? → **409**.
4. Has this student already submitted? → **409**.
5. Insert the row. If the database's unique index refuses it → **409**.
6. Read it back and return it.

**Step 6 — Repository.** `SubmissionRepository` is the only place EF Core appears.
It turns those requests into SQL.

**Step 7 — Back out.** The service returns a `Result` object; the controller turns
it into a status code and JSON body.

The shape to remember: **the controller decides nothing, the service decides
everything, the repository knows nothing except how to fetch and save.**

---

## 7. The two custom pieces you should be able to explain

### `Result<T>` — reporting failure as a value

A normal C# method reports failure by *throwing an exception*. We do not. Every
service method returns a `Result<T>`, which carries either a value or one of five
outcomes:

```csharp
public enum ResultStatus
{
    Success,
    ValidationFailed,   // → 400
    Forbidden,          // → 403
    NotFound,           // → 404
    Conflict,           // → 409
    Unprocessable       // → 422
}
```

**Why not exceptions?** Two reasons, and both are things a reviewer might push on:

1. The 403-versus-404 decision (§8) is a *rule*, and it belongs next to the rule
   that motivates it — not in a middleware in a different project.
2. A test can assert `result.Status.Should().Be(ResultStatus.NotFound)`, which is a
   statement about behaviour. `Assert.Throws<NotFoundException>` is a statement
   about plumbing.

The enum-to-status-code mapping lives in exactly **one** place —
`ApiControllerBase.ErrorResponse`. No individual endpoint has to remember it, which
is why the discipline actually holds across 30-odd endpoints.

### `TimeProvider` — an injectable clock

Anywhere the code needs the current time, it does *not* call `DateTime.UtcNow`. It
calls `_timeProvider.GetUtcNow()`, where `TimeProvider` is handed in by dependency
injection.

**Why?** Because "reject submissions after the deadline" is untestable with a real
clock — you would have to actually wait. With an injected clock, the test sets the
time to *exactly* the deadline, then one tick past it, and asserts both. That is how
we know the boundary is right rather than roughly right.

`TimeProvider` is built into .NET 8. In production it is `TimeProvider.System`; in
tests it is our 8-line `FakeTimeProvider` with a settable `UtcNow`.

**Dependency injection**, if that phrase is new: instead of a class creating the
things it needs (`new AssignmentRepository()`), it declares them in its constructor
and the framework supplies them at runtime. The wiring is in the two
`DependencyInjection.cs` files. `AddScoped` means "one instance per HTTP request",
`AddSingleton` means "one for the whole application".

---

## 8. Status codes: the part worth rehearsing

We use six, each with exactly one meaning:

| Code | Means | Example |
| --- | --- | --- |
| **400** | The request is wrong *on its own terms* | Empty title, `maxMarks: 0` |
| **401** | No usable token | Missing, expired or tampered |
| **403** | Wrong role, or no standing over this class/subject at all | Student hitting a teacher route |
| **404** | Absent, **or not yours** | Someone else's assignment |
| **409** | Well-formed, but the current *state* forbids it | Deadline passed; already published |
| **422** | Well-formed and allowed, but a value is out of range *for this resource* | 11 marks on a 10-mark assignment |

### Why "not yours" is 404 and not 403

This is the design decision most likely to come up, so here is the reasoning in
full.

Suppose teacher2 sends `PUT /api/assignments/{some-id}`. If we answered **403**
("not yours"), we would have confirmed that the id names a *real* assignment.
Teacher2 could then loop through ids and map out everything their colleagues have
created, purely by watching which ones say 403 and which say 404.

So both answers are **404**, with the same message: `"Assignment not found."` We do
not just do this by convention — the message is a single shared constant
(`NotFoundMessages.Assignment`), and a test asserts that four different reasons
produce one identical string.

The two cases *are* distinguished in the **server log**, where "not yours" is logged
as a warning and "does not exist" as information. The operator can tell them apart;
the caller cannot. That is the point.

**When 403 is right:** creating an assignment for a class you do not teach. There,
*you* supplied the class and subject ids out of admin-managed lists, so naming them
back leaks nothing you did not already know — and a specific message is the
difference between a usable error and a dead end.

This exact reasoning is the same one behind the login endpoint: a wrong email and a
wrong password return a byte-identical 401, so nobody can discover which email
addresses have accounts.

### Why 422 exists

`{"marks": 11}` is *valid* against an assignment worth 20 and *invalid* against one
worth 10. So no request validator can judge it — the answer depends on which
assignment you are grading. That is precisely what 422 is for: understood,
well-formed, and unprocessable against this particular target.

Consequence worth knowing, because it looks like an omission: `GradeSubmissionRequest.Marks`
has **no validation rule at all**. Adding a partial one (rejecting negatives at the
edge) would mean `-1` returns 400 while `999` returns 422 — same underlying reason,
two different status codes, no logic a caller could follow. The whole range check
lives in the service and the message names the real maximum.

---

## 9. The business rules

Fifteen rules, all in the two service classes, all unit-tested.

### Assignments ([full detail](assignments.md))

| # | Rule | Fails with |
| --- | --- | --- |
| 1 | A teacher may only create work for a class+subject they hold a `TeacherAssignment` for | 403 |
| 2 | A teacher may only update, publish or delete **their own** assignments | 404 |
| 3 | Max marks between 1 and 1000 | 400 |
| 4 | Publishing needs a deadline **strictly** in the future | 409 |
| 5 | Once published: title and description editable, deadline may only move **later**, class/subject/marks frozen | 409 |
| 6 | Published never goes back to draft | not expressible |
| 7 | Only a draft may be deleted | 409 |
| 8 | Drafts are invisible to students everywhere | 404 |

### Submissions ([full detail](submissions.md))

| # | Rule | Fails with |
| --- | --- | --- |
| 1 | Students submit only to published work for their own class | 404 |
| 2 | Nothing submitted or revised after the deadline | 409 |
| 3 | One submission per student per assignment | 409 |
| 4 | A student cannot revise reviewed work, even before the deadline | 409 |
| 5 | Teachers see and grade only submissions on their own assignments | 404 |
| 6 | Marks between 0 and that assignment's max, inclusive | 422 |
| 7 | Answer required, capped at 20,000 characters | 400 |

### Four of these have a story behind them

**Rule 6 (assignments) has no `if` statement.** There is nothing to check. The
update request DTO has no status field, and `UpdateAsync` never assigns to `Status`
— the only status write anywhere in the service is `Draft → Published` inside
`PublishAsync`. So there is no code path to guard, and writing a guard would be dead
code. Two tests hold the line: one asserts the DTO has no status property, one
asserts that editing a published assignment leaves it published. If someone later
wants an "unpublish", they have to *deliberately add a field*, which is exactly the
friction we want.

**Rule 1 (assignments) is checked twice.** The brief only implies it on create. But
a teacher could create a legitimate draft for a class they do teach, then `PUT` it at
a class they do not — straight around the create-time check. So the entitlement is
re-checked on update whenever a draft's class or subject actually changes.

**Rule 3 (submissions) is enforced twice, and the second time is the one that
counts.** The service checks "has this student already submitted?" first, because
that gives a helpful message. But a student who double-clicks can get two requests
past that check — only the database can settle a race. So the real authority is the
unique index on `(AssignmentId, StudentId)`. `SubmissionRepository.TryAddAsync`
catches Postgres error code `23505` and returns `false`, which the service turns
into the same 409. A lost race reads as a duplicate, never as a 500 crash.

Note where that translation lives: in **Infrastructure**. The Application layer must
not know what Npgsql is, so the constraint violation is converted into a plain
`bool` on the way out. That is the layering rule from §3 doing real work.

**The two deadline boundaries are deliberately opposite.** Submitting *exactly* at
the deadline is **on time**. Publishing *exactly* at the deadline is **rejected**.
Side by side that looks like a bug. It is not: publishing work that is already due
gives students no time at all, whereas a student who makes the deadline to the
instant has made it. Both are tested at the boundary and one tick either side.

---

## 10. Testing

**127 tests, all passing, about one second.**

| Test class | Cases | Covers |
| --- | --- | --- |
| `SubmissionServiceTests` | 43 | submission rules |
| `AssignmentServiceTests` | 40 | assignment rules |
| `SubmissionRepositoryTests` | 13 | query composition |
| `AssignmentRepositoryTests` | 11 | query composition |
| `AuthServiceTests` | 9 | login behaviour |
| `JwtTokenGeneratorTests` | 5 | token claim contract |
| `AssignmentQueriesTests` | 4 | student visibility predicate |
| `SolutionSmokeTests` | 2 | harness works |
| | **127** | |

One small thing that is easy to get wrong if you quote numbers: these are **test
cases**, not test *methods*. There are 118 methods, because a `[Theory]` method runs
once per row of data — `GradeAsync_WithinTheAssignmentsRange_IsAccepted` is one
method that produces three cases (0, 5 and 10 marks). If someone counts `[Fact]`
attributes in the files they will get 118, not 127, and both numbers are correct
about different things.

Run them with:

```bash
dotnet test backend/AssignmentHub.sln
```

Three layers of testing, each answering a different question:

**Unit tests (103)** — do the rules behave? No database, no HTTP. The store is a
`List<>` and the clock is frozen. Fast enough to run constantly. (Two of the 103 are
just harness smoke tests from the very first commit, kept because they prove the
project reference graph reaches every layer.)

**Repository tests (24)** — are the *queries* built correctly? These use a real
`AppDbContext` on EF Core's in-memory provider, and cover things a fake store cannot
vouch for — for instance that the admin's `classRoomId` filter reaches *through* the
parent assignment, since a submission has no class of its own.

**Manual API verification** — does it work against real Postgres? Scripted passes of
97 checks (assignments) and 118 checks (submissions) against the running API.

### Be able to say what the tests do *not* prove

This matters more than the count, and it is the sort of thing a good reviewer
respects:

- The in-memory provider runs LINQ against objects, so repository tests prove the
  queries are *composed* right, **not** that Npgsql translates them to correct SQL.
  Real Postgres is covered by the manual passes.
- The in-memory provider **does not enforce unique indexes**, so the duplicate-insert
  path cannot be reached there at all.
- There are **no HTTP-level integration tests** yet. `Microsoft.AspNetCore.Mvc.Testing`
  is referenced and ready, but the role gates are currently verified by hand rather
  than automatically.

### One real thing that happened, worth telling honestly

The concurrent-submit check (rule 3) was run with 8 simultaneous requests. Result: 1
× 201, 7 × 409, no crashes. Looked like a pass.

But checking the server log showed the unique index had **never fired** — the
service's own pre-check had absorbed all seven. So the database-race path was still
unproven. Re-running with 24 requests released simultaneously off a barrier:

```
1 × 201, 23 × 409, no 5xx
5 requests reached SQLSTATE 23505 and were mapped to 409
```

That is the path actually proven. The lesson generalises: a green test can prove
less than it appears to, and the log is where you find out.

### One deliberately weak assertion that got fixed

The admin listing check originally asserted "admin sees more than one class" — but
every seeded submission is Class 9-A, so it passed trivially. Fixed by having a
Class 10-A student submit first, so the assertion has something real to check. Worth
mentioning because finding it required reading the *output*, not the pass count.

---

## 11. What is done and what is not

### Done

- Project structure, Docker Postgres, config and secret handling
- Six entities, EF Core mappings, `InitialSchema` migration, idempotent seeder
- JWT login, `/me`, role-based authorization
- Assignments: teacher CRUD + publish, student read, admin read
- Submissions: student submit/revise/view, teacher list/grade/reopen, admin view
- 127 tests; four feature docs

### Not done

- **Frontend.** Scaffolded (Next.js, TypeScript, Tailwind, a working health-check
  page) but no real screens. This is the largest remaining chunk.
- **Admin management endpoints.** The admin can *view* everything but cannot yet
  create users, classes, subjects or teacher assignments through the API. The brief
  asks for this. Right now those rows come from the seeder.
- **README.** Still the skeleton. The brief explicitly requires overview, features,
  setup, how to run tests, demo credentials, assumptions and known limitations. The
  four docs in `docs/` are the raw material.
- **HTTP integration tests.**
- Three known issues carried deliberately: `npm audit` reports 3 high-severity
  transitive advisories in the Next.js dependency tree (fixable only by forcing a
  Next downgrade); `frontend/CLAUDE.md` and `frontend/AGENTS.md` are
  `create-next-app` boilerplate still tracked; no pagination anywhere.

### Deliberate scope decisions, all written down

Every one of these is documented in the feature docs rather than left silent:

- No refresh tokens, logout, rate limiting or password reset (auth.md)
- No file uploads on submissions — answers are text
- No unpublish or withdraw for a published assignment
- No audit trail on who changed a submission's status
- Reopening a submission keeps its marks, so it can read `Submitted` while carrying
  8/10 — intentional, and a frontend should present it as "previously marked,
  awaiting re-marking"

---

## 12. Questions you should have an answer ready for

**"Walk me through what happens when a student submits an answer."**
§6. The short version: middleware validates the token → role attribute checks
Student → FluentValidation checks the body shape → controller reads the caller id
from the token and calls the service → service checks class visibility, deadline,
duplicate → repository inserts → result maps back to a status code.

**"Why 404 and not 403 when a teacher touches someone else's assignment?"**
§8. Because 403 confirms the id is real, which turns the endpoint into a way to
enumerate colleagues' work. The message is a shared constant so the two cases cannot
drift apart, and the difference is preserved in the log instead.

**"Where would I add a rule that says X?"**
In the relevant `Service` class in the Application project, plus a unit test. Not in
the controller, and not in the repository.

**"How do you know the deadline check is right at the boundary?"**
The clock is injected (`TimeProvider`), so tests set it to exactly the deadline, one
tick before, and one tick after. And the two boundaries are deliberately different
directions for submitting versus publishing — §9.

**"What stops two submissions from the same student?"**
A service check for the friendly message, and a unique database index as the real
authority. 24 simultaneous requests produce exactly one 201; 5 of them reached the
index and came back as 409, not 500 — §10.

**"What are you least confident about?"**
Honest answers: no HTTP-level integration tests, so role gates are hand-verified;
the repository tests use an in-memory provider that cannot prove Npgsql translation
or enforce unique indexes; and the frontend is the biggest unknown because none of
it is built yet.

**"Which decisions could reasonably have gone the other way?"**
Good ones to raise yourself, because they show you understand the trade-off rather
than just the choice:

- **Publishing twice returns 409 rather than being idempotent.** Defensible either
  way. We chose 409 because publishing is an *event* — it makes work visible to a
  class, and it is where a notification would fire — so a silent second success
  would hide a double-submitting client. The cost is that a client which loses the
  response must read before retrying.
- **Reopening a submission keeps the marks.** The alternative — clearing them —
  is also reasonable. We kept them because the work really *was* marked, and the
  student should keep seeing the feedback that told them what to fix.
- **Repository per aggregate, rather than a generic `IRepository<T>`.** More files,
  but each interface only exposes the queries actually needed, which keeps service
  tests to a couple of fake methods instead of a whole ORM surface.
- **`Result<T>` instead of exceptions.** Exceptions are the more common .NET choice.
  We went the other way for the two reasons in §7.
