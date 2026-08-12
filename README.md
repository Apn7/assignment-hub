# Assignment Hub — Role-Based Assignment & Submission Management System

[![Build & Test](https://img.shields.io/badge/Unit%20Tests-145%20Passed-brightgreen)](https://github.com/Apn7/assignnment-hub)
[![Framework](https://img.shields.io/badge/.NET-8.0%20Web%20API-512BD4)](https://dotnet.microsoft.com/)
[![Frontend](https://img.shields.io/badge/Next.js-16%20App%20Router-000000)](https://nextjs.org/)
[![Design](https://img.shields.io/badge/Design-Paper%20Theme-FBF9F5)](https://github.com/Apn7/assignnment-hub)

A role-based school/college Assignment & Submission Management System built with **ASP.NET Core 8 Web API** (Clean Architecture), **Next.js 16 App Router** (React 19 + Anthropic-inspired Paper Theme UI), and **PostgreSQL 16**.

---

## 📌 Project Links & Demo Credentials

- **GitHub Repository**: [https://github.com/Apn7/assignnment-hub.git](https://github.com/Apn7/assignnment-hub.git)
- **Local Application URL**: `http://localhost:3000`
- **API Swagger Documentation**: `http://localhost:5080/swagger`
- **API Health Check**: `http://localhost:5080/api/health`

### Working Demo Credentials

| Role | Email | Password | Landing Portal & Capabilities |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@assignmenthub.local` | `Admin#1234` | `/admin` — System management (Users, Classes, Subjects, Teacher Entitlements, Audit Tables) |
| **Teacher 1** | `teacher1@assignmenthub.local` | `Teacher#1234` | `/teacher` — Teaches Class 9 – A (Physics) |
| **Teacher 2** | `teacher2@assignmenthub.local` | `Teacher#1234` | `/teacher` — Teaches Class 10 – A (Higher Mathematics) |
| **Student 1** | `student1@assignmenthub.local` | `Student#1234` | `/student` — Student in Class 9 – A |
| **Student 2** | `student2@assignmenthub.local` | `Student#1234` | `/student` — Student in Class 9 – A |
| **Student 3** | `student3@assignmenthub.local` | `Student#1234` | `/student` — Student in Class 10 – A |
| **Student 4** | `student4@assignmenthub.local` | `Student#1234` | `/student` — Student in Class 10 – A |

---

## ✨ Features

### 👑 Admin Portal (`/admin`)
- **User Management**: Create Admin, Teacher, and Student accounts (`POST /api/admin/users`). Enforces role rules (Students require a Class; Teachers/Admins must not have one). Email lowercasing on create closes case-sensitivity gaps.
- **Class & Subject Management**: Create Classes (`POST /api/admin/classrooms`) and Subjects (`POST /api/admin/subjects`) with case-insensitive duplicate prevention.
- **Teacher Assignments**: Entitle teachers to teach specific class/subject pairs (`POST /api/admin/teacher-assignments`) with duplicate triple index protection.
- **System Audit View**: System-wide overview of all assignments and student submissions across all classes and subjects with status filtering.

### 👩‍🏫 Teacher Portal (`/teacher`)
- **Entitlement-Scoped Assignments**: Teachers select only from their assigned teaching pairs (`GET /api/teacher-assignments/mine`).
- **Assignment Lifecycle**: Create, Edit, Publish (`Draft` vs `Published`), and Delete assignments. Set Title, Description, UTC Deadline, and Max Marks.
- **Submission Review & Grading**: View student submissions for published assignments, award marks (`0 <= marks <= maxMarks`), and provide detailed feedback.
- **Status Workflow**: Ability to reopen reviewed submissions back to `Submitted` for revision.

### 🎓 Student Portal (`/student`)
- **Class-Scoped Feed**: View published assignments for the student's assigned class ordered by nearest deadline.
- **Submission Workflow**: Submit answer text before the deadline (`POST /api/assignments/{id}/submissions`).
- **Revision Before Deadline**: Update existing submission prior to the deadline (`PUT /api/assignments/{id}/submissions/mine`).
- **Grade & Feedback View**: Read-only submission status, score ratio, and teacher feedback.

---

## 🎨 Design System: Anthropic Paper Theme

The frontend is styled using an **Anthropic-inspired Paper Theme**:
- **Typography**: Google Fonts **Lora** (elegant serif headings) paired with **Plus Jakarta Sans** (clean sans-serif body).
- **Color Palette**:
  - Warm Paper Canvas: `#FBF9F5`
  - Elevated Card Surfaces: `#FFFFFF` with soft `#E6E2D6` borders
  - Primary Ink: `#1F1D1A`
  - Secondary Text: `#45413C` & `#7C766C`
- **UI Components**: Custom paper status pills (`Draft`, `Published`, `Submitted`, `Reviewed`), backdrop blur modals, drawer panels, and clean empty/loading/error states.

---

## 🛠️ Technology Stack

| Layer | Technology Choice |
| :--- | :--- |
| **Frontend Framework** | Next.js 16 (App Router), React 19, TypeScript |
| **Styling** | Vanilla CSS / Tailwind CSS with Paper Tokens, Lora & Plus Jakarta Sans fonts |
| **State & Data Fetching** | TanStack Query (@tanstack/react-query), Axios with JWT interceptors |
| **Forms & Validation** | React Hook Form + Zod (`@hookform/resolvers`) |
| **Backend Framework** | ASP.NET Core 8 Web API, C#, Clean Architecture |
| **Application Logic** | `Result<T>` pattern, FluentValidation, Serilog logging |
| **Database** | PostgreSQL 16 via Entity Framework Core 9 (Npgsql), UTC Converters |
| **Authentication** | JWT Bearer Tokens, PBKDF2 `IPasswordHasher`, Role Guards (`RequireRole`) |
| **Testing** | xUnit, Moq, FluentAssertions, EF Core In-Memory Provider (**145 Passing Tests**) |

---

## 📂 Project Structure

```
assignment-hub/
├── backend/
│   ├── AssignmentHub.sln
│   ├── src/
│   │   ├── AssignmentHub.Api/            # Thin Controllers, JWT setup, Middleware
│   │   ├── AssignmentHub.Application/    # Services, DTOs, FluentValidation, Result<T>
│   │   ├── AssignmentHub.Domain/         # Domain Entities (User, ClassRoom, Subject, etc.)
│   │   └── AssignmentHub.Infrastructure/ # EF Core DbContext, Repositories, PasswordHasher
│   └── tests/
│       └── AssignmentHub.Tests/          # 145 Unit tests (xUnit + Moq + FluentAssertions)
├── frontend/                             # Next.js App Router application
│   └── src/
│       ├── app/                          # Role layouts (/admin, /teacher, /student, /login)
│       ├── components/                   # Paper UI primitives (StatusBadge, ConfirmDialog)
│       ├── lib/api/                      # Axios client, JWT interceptors, error helpers
│       ├── lib/auth/                     # Auth storage & token decoding helpers
│       └── types/                        # TypeScript DTO interfaces
├── docs/                                 # Architectural docs & requirement specifications
├── docker-compose.yml                    # PostgreSQL 16 container setup
└── README.md
```

---

## 🚀 Quick Start Guide

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20.9+](https://nodejs.org) and `npm`
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for PostgreSQL)

---

### 1. Database Setup

Start the PostgreSQL database container:

```bash
docker compose up -d db
```

The database seeds automatically on startup when the backend initializes (`DataSeeder`).

---

### 2. Backend API Setup

Run the backend server:

```bash
cd backend
dotnet run --project src/AssignmentHub.Api/AssignmentHub.Api.csproj
```

- **Swagger UI**: [http://localhost:5080/swagger](http://localhost:5080/swagger)
- **Health Check**: [http://localhost:5080/api/health](http://localhost:5080/api/health)

---

### 3. Frontend Web Application Setup

In a new terminal window:

```bash
cd frontend
npm install
npm run dev
```

Open [http://localhost:3000](http://localhost:3000) in your browser.

---

## 🧪 Running Automated Tests

Run the complete unit test suite across application services, repositories, authorization rules, and validation logic:

```bash
cd backend
dotnet test --verbosity normal
```

**Test Execution Summary**:
- **Total Tests**: 145
- **Passed**: 145
- **Failed**: 0
- **Time**: ~7 seconds

---

## 🔒 Security & Architecture Principles

1. **Backend Security, Frontend UX**: Frontend role guards (`RequireRole`) provide clean UX by hiding unauthorized actions; the ASP.NET Core API strictly validates tokens, roles, and entity ownership on every request.
2. **Explicit Result Pattern**: Application services return `Result<T>` data objects instead of throwing exceptions. `ApiControllerBase` centrally maps status kinds (`ResultStatus.Forbidden` -> `403`, `ResultStatus.NotFound` -> `404`, `ResultStatus.Unprocessable` -> `422`, `ResultStatus.Conflict` -> `409`).
3. **2-Layer Conflict Discipline**: Duplicate checks use an Application pre-check for clean error messages AND a database-level `23505` `PostgresException` catch block in repositories for race condition protection.

---

## 📝 Documented Assumptions & Descope Decisions

- **Admin Management Descope**: Admin CRUD endpoints support **CREATE and LIST** operations. Update and Delete operations for users, classes, subjects, and teacher entitlements are intentionally descoped.
- **Deadline Strictness**: Students cannot submit or edit answers after `deadline` UTC timestamp.
- **Single Submission per Student**: Each student has exactly one submission per assignment, editable prior to deadline.
- **Case-Insensitive Email Normalization**: User emails are normalized to lowercase on creation, closing potential case-sensitivity login conflicts.
