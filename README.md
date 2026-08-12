# Assignment Hub — Role-Based Assignment & Submission Management System

[![Build & Test](https://img.shields.io/badge/Unit%20Tests-145%20Passed-brightgreen)](https://github.com/Apn7/assignnment-hub)
[![Backend](https://img.shields.io/badge/.NET-8.0%20Web%20API-512BD4)](https://dotnet.microsoft.com/)
[![Frontend](https://img.shields.io/badge/Next.js-16%20App%20Router-000000)](https://nextjs.org/)
[![Design](https://img.shields.io/badge/Design-Paper%20Theme-FBF9F5)](https://github.com/Apn7/assignnment-hub)

A role-based school/college Assignment & Submission Management System built for the **Assistant Software Engineer Recruitment Project** (OnnoRokom Projukti Limited).

---

## 📌 Submission Information

- **Git Repository Link**: [https://github.com/Apn7/assignnment-hub.git](https://github.com/Apn7/assignnment-hub.git)
- **Submission Form Link**: [https://q-rp.com/c/4CIs](https://q-rp.com/c/4CIs)

---

## 🔑 Demo Credentials

Working credentials for all three roles (seeded automatically on database startup):

| Role | Email | Password | Assigned Scope / Landing |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@assignmenthub.local` | `Admin#1234` | System portal (`/admin`) — Manage Users, Classes, Subjects, Teacher Entitlements, Audit Tables |
| **Teacher** | `teacher1@assignmenthub.local` | `Teacher#1234` | Teacher portal (`/teacher`) — Teaches Class 9 – A (Physics) |
| **Student** | `student1@assignmenthub.local` | `Student#1234` | Student portal (`/student`) — Enrolled in Class 9 – A |

*Additional Seed Accounts*:
- **Teacher 2**: `teacher2@assignmenthub.local` / `Teacher#1234` (Class 10 – A, Higher Mathematics)
- **Student 2-4**: `student2@assignmenthub.local` to `student4@assignmenthub.local` / `Student#1234`

---

## 📖 1. Project Overview

**Assignment Hub** is a role-based web application that streamlines assignment management between teachers, students, and administrators. 

- **Teachers** create assignments for specific class/subject pairs, set deadlines and maximum marks, publish drafts, view student submissions, award marks, provide feedback, and reopen submissions for revision when necessary.
- **Students** view published assignments for their enrolled class, submit answers before the UTC deadline, edit submissions prior to deadline expiration, and view teacher feedback & marks.
- **Admins** manage users (Admin, Teacher, Student), create classes and subjects, assign teachers to class/subject pairs, and audit all system assignments & submissions.

---

## ✨ 2. Main Features

### Admin Management (`/admin`)
- **User Creation**: Create Admin, Teacher, and Student accounts (`POST /api/admin/users`). Enforces role rules (Student requires Class; Teacher/Admin forbids Class). Email lowercasing on create closes case-sensitivity gaps.
- **Class & Subject Management**: Create Classes (`POST /api/admin/classrooms`) and Subjects (`POST /api/admin/subjects`) with case-insensitive duplicate prevention (409).
- **Teacher Entitlements**: Assign teachers to class/subject pairs (`POST /api/admin/teacher-assignments`) with duplicate triple index protection (409).
- **System Audit View**: Overview of all assignments and submissions across all classes and subjects with status filtering.

### Teacher Management (`/teacher`)
- **Entitlement-Scoped Work**: Teachers select only from their assigned teaching pairs (`GET /api/teacher-assignments/mine`).
- **Assignment Lifecycle**: Create, edit, publish (`Draft` vs `Published`), and delete assignments.
- **Submissions & Grading**: Review student submissions, grade (`0 <= marks <= maxMarks`), and provide text feedback. Reopen reviewed submissions back to `Submitted` for student revision.

### Student Management (`/student`)
- **Class-Scoped Feed**: View published assignments for the student's enrolled class ordered by nearest deadline.
- **Submission Workflow**: Submit answer text before the UTC deadline (`POST /api/assignments/{id}/submissions`).
- **Revision Before Deadline**: Edit existing submission prior to deadline expiration (`PUT /api/assignments/{id}/submissions/mine`).
- **Grade & Feedback View**: Read-only submission status badge, score ratio (`marks / maxMarks`), and teacher feedback box.

---

## 🎨 Design System: Anthropic Paper Theme

Styled with an **Anthropic-inspired Paper Theme**:
- **Typography**: Google Fonts **Lora** (headings) + **Plus Jakarta Sans** (body).
- **Color Tokens**: Warm paper canvas `#FBF9F5`, white cards `#FFFFFF` with `#E6E2D6` borders, deep ink `#1F1D1A` text.
- **UI Components**: Paper status badges (`Draft`, `Published`, `Submitted`, `Reviewed`), backdrop blur modals, drawers, and responsive layouts.

---

## 🛠️ 3. Technology Stack

| Layer | Technology Choice |
| :--- | :--- |
| **Frontend** | Next.js 16 (App Router), React 19, TypeScript |
| **Styling** | Vanilla CSS & Tailwind CSS (Paper Tokens), Lora & Plus Jakarta Sans fonts |
| **Forms & State** | React Hook Form + Zod (`@hookform/resolvers`), TanStack Query, Axios |
| **Backend** | ASP.NET Core 8 Web API, C#, Clean Architecture |
| **Logic & Validation** | `Result<T>` pattern, FluentValidation, Serilog logging, Swagger/OpenAPI |
| **Database** | PostgreSQL 16 via Entity Framework Core 9 (Npgsql), UTC Converters |
| **Auth & Security** | JWT Bearer Tokens, PBKDF2 `IPasswordHasher`, Role Guards (`RequireRole`) |
| **Testing** | xUnit, Moq, FluentAssertions (**145 Passing Unit Tests**) |

---

## 📂 4. Project Structure

```
assignment-hub/
├── backend/
│   ├── AssignmentHub.sln
│   ├── src/
│   │   ├── AssignmentHub.Api/            # Controllers, Swagger, JWT Auth, Middleware
│   │   ├── AssignmentHub.Application/    # Services, DTOs, FluentValidation, Result<T>
│   │   ├── AssignmentHub.Domain/         # Domain Entities & Enums
│   │   └── AssignmentHub.Infrastructure/ # EF Core DbContext, Repositories, Migrations
│   └── tests/
│       └── AssignmentHub.Tests/          # 145 Unit tests (xUnit + Moq + FluentAssertions)
├── frontend/                             # Next.js App Router application
│   └── src/
│       ├── app/                          # Role portals (/admin, /teacher, /student, /login)
│       ├── components/                   # Paper UI primitives (StatusBadge, ConfirmDialog)
│       ├── lib/api/                      # Axios client with JWT interceptors
│       ├── lib/auth/                     # Auth storage & token decoding helpers
│       └── types/                        # TypeScript DTO contracts
├── docs/                                 # Documentation reference specs
├── docker-compose.yml                    # PostgreSQL 16 service configuration
├── .env.example                          # Root environment template
└── README.md
```

---

## 🚀 5. Setup Instructions (Easy Local Setup)

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20.9+](https://nodejs.org) and `npm`
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for PostgreSQL 16)

---

### Database Setup Instructions

1. Start the PostgreSQL 16 container:

   ```bash
   docker compose up -d db
   ```

2. Database table schemas and initial demo data seed automatically on backend startup via EF Core migrations and `DataSeeder`. No manual database table creation is needed.

---

### Instructions for Running the Backend

From the repository root:

```bash
cd backend
dotnet run --project src/AssignmentHub.Api/AssignmentHub.Api.csproj
```

- **Backend API Base URL**: `http://localhost:5080`
- **Swagger API Documentation**: `http://localhost:5080/swagger`
- **API Health Check**: `http://localhost:5080/api/health`

---

### Instructions for Running the Frontend

In a new terminal window:

```bash
cd frontend
npm install
npm run dev
```

Open [http://localhost:3000](http://localhost:3000) in your browser.

---

### Instructions for Running the Tests

To execute the complete unit test suite across business rules, authorization, and submission workflows:

```bash
cd backend
dotnet test --verbosity normal
```

**Test Output**: **145 Passed**, 0 Failed, 0 Skipped (~7s execution time).

---

## ⚙️ 6. Environment Configuration

Sensitive credentials are excluded from source control. Environment variables are loaded via `.env.example` templates:

- Root template: `.env.example` (`POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`)
- Frontend template: `frontend/.env.local.example` (`NEXT_PUBLIC_API_URL=http://localhost:5080`)

---

## 📋 7. Assumptions

1. **Deadline Enforcement**: All deadlines are stored and validated in UTC (`DateTime.UtcNow`). Students cannot submit or edit after the deadline.
2. **Single Submission per Student**: Each student has exactly one submission per assignment, which remains editable until the deadline.
3. **Teaching Pair Scoping**: Teachers may only create assignments for class/subject pairs assigned to them in `TeacherAssignments`.
4. **Email Normalization**: Login and user creation normalize emails to lower-case.

---

## 🚧 8. Known Limitations

1. **Admin Management Scope**: Admin CRUD endpoints support **CREATE and LIST** operations. Update and Delete operations for users, classes, subjects, and entitlements are intentionally descoped.
2. **No Refresh Tokens**: Relies on stateless 60-minute JWT access tokens.
3. **No File Uploads**: Submissions accept structured answer text formatted in markdown/text as per the initial requirements.
