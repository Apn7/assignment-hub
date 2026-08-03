# Assignment & Submission Management System — Requirements

> **Source of truth.** Transcribed from _"Assistant Software Engineer Recruitment
> Project — Assignment & Submission Management System"_ (OnnoRokom Projukti
> Limited, 4 pages, produced with ONLYOFFICE 9.4.0.129).
>
> Extracted from the PDF's embedded text layer with PyMuPDF, page by page,
> including table structure and hyperlinks. The document is born-digital, not a
> scan, so the text below is exact rather than OCR-inferred.
>
> Sections 1–6 are the client's wording. Anything under
> [Derived requirements](#derived-requirements-not-from-the-pdf) is our own
> inference and carries no authority.

A role-based school/college application for evaluating understanding of
requirements, system design, API development, frontend implementation, and
testing.

| | |
| --- | --- |
| **Project type** | Full-stack web application |
| **Submission deadline** | **14 August, 2026** |

> Please read the requirements carefully and make reasonable assumptions where
> the requirements are not explicitly defined. **Document those assumptions in
> the README.**

---

## 1. Project Brief

Build a role-based Assignment & Submission Management System for a school or
college. The system should allow teachers to create assignments for specific
classes or courses, students to view and submit assignments, and teachers to
review submissions and provide marks and feedback.

## 2. User Roles and Responsibilities

> Applicants may use a different but suitable design. Any important design
> decisions should be explained in the README.

### Admin

- Manage users.
- Manage classes/courses and subjects.
- Assign teachers to subjects/classes.
- View all assignments and submissions.
- Manage application-level settings where necessary.

### Teacher

- Create, update, and delete assignments.
- Assign an assignment to a specific class/course and subject.
- Define the title, description, deadline, and maximum marks.
- Publish an assignment or keep it as a draft.
- View student submissions.
- Assign marks and provide feedback.
- Change the submission status when necessary.

### Student

- View assignments assigned to their class/course.
- View assignment details and deadline.
- Submit an answer.
- Update a submission before the deadline, if allowed.
- View submission status, marks and teacher feedback.

## 3. Technical Requirements

Use the following technologies, or equivalent technologies suitable for the
project:

| Layer | Requirement |
| --- | --- |
| **Frontend** | Next.js, React, TypeScript, responsive UI, form validation and API integration |
| **Backend** | ASP.NET Core Web API, C#, RESTful API, validation, error handling, logging and Swagger/OpenAPI |
| **Database** | PostgreSQL or MongoDB. Implement the required relationships, or explain the chosen data model. |
| **Authentication** | Login, JWT-based authentication, and role-based authorization |
| **Testing** | Unit tests covering important business rules, authorization, and submission workflows |

## 4. Submission Guidelines

After completing the project, please submit the following:

**Git repository link** — Submit a GitHub or GitLab repository link containing
the complete source code.

**Complete project code** — Include the frontend, backend/API, database files,
and unit tests in the repository.

**Database files** — Include migration files, seed/sample data, and a database
script or backup file, if applicable. The evaluator should be able to set up the
database **without manually creating tables or collections**.

**README.md** — Include a short project overview, main features, technology
stack, project structure, setup instructions, database setup instructions,
instructions for running the frontend and backend, instructions for running the
tests, assumptions, and known limitations.

**Demo credentials** — Provide working login credentials for the Admin, Teacher,
and Student roles.

| Role | Email | Password |
| --- | --- | --- |
| Admin | | |
| Teacher | | |
| Student | | |

**Environment configuration** — Do not upload real passwords, API keys, or other
sensitive information. Include an `.env.example` file showing the required
environment variables.

**Easy local setup** — Provide clear and complete setup instructions in the
README so the project can be run locally.

### Optional additions

A live project URL, API/Swagger URL, Docker configuration, pagination, advanced
filtering, notifications, or other additional features may be included, but they
are **not mandatory**.

## 5. Final Checklist

Before submitting, please confirm the following:

- [ ] The repository link is accessible.
- [ ] Frontend and backend are both included.
- [ ] The database can be created using the provided files or instructions.
- [ ] Demo accounts for all three roles are available.
- [ ] The README explains how to run the project and its tests.
- [ ] Role-based access is enforced by the backend API.
- [ ] Important business rules are implemented and tested.
- [ ] No real secrets or credentials are committed to the repository.

## 6. Project Submission

After completing the project, please submit it through the following link:
<https://q-rp.com/c/4CIs>

If you face any issues while submitting your project, please contact us at
<hrd@onnorokom.com>.

---

## Derived requirements (NOT from the PDF)

Our own reading of what the wording above implies. Useful for planning; not
quotable as client requirement.

### Implied domain model

The role responsibilities imply these entities: `User` (with a role), `Class`
(or Course), `Subject`, a teacher↔subject/class assignment, `Assignment`, and
`Submission`. Two enums are implied directly by the wording — assignment status
(**draft / published**, from "Publish an assignment or keep it as a draft") and
submission status (from "Change the submission status when necessary" and "View
submission status").

An `Assignment` carries title, description, deadline and maximum marks — those
four are named explicitly. A `Submission` carries an answer, a status, marks and
feedback.

### Business rules worth testing

The checklist demands "important business rules are implemented and tested", and
the Testing row names authorization and submission workflows specifically. The
rules the wording actually supports:

1. A student sees only assignments for **their own** class/course.
2. A student sees only **published** assignments — never drafts.
3. A student may update a submission **only before the deadline** ("if allowed"
   leaves room for a teacher-controlled or config-controlled toggle — document
   whichever is chosen).
4. Marks awarded cannot exceed the assignment's maximum marks.
5. A teacher may only act on assignments for classes/subjects they are assigned
   to.
6. Only a teacher may assign marks and feedback; only a teacher may change
   submission status.
7. Only an admin may manage users, classes/courses, subjects and teacher
   assignments.
8. Every rule above must be enforced **server-side** — "Role-based access is
   enforced by the backend API" makes frontend-only guarding insufficient.

### Consequences for delivery

- **Seed data is mandatory, not optional.** "Demo credentials" plus "the
  evaluator should be able to set up the database without manually creating
  tables" means migrations *and* a seeder that creates one working account per
  role.
- **Assumptions must be written down.** Requested twice — once for undefined
  requirements, once for design decisions that deviate from the suggested role
  design.
- **Docker is optional.** Listed under optional additions, so `docker-compose`
  is a bonus rather than a requirement.
- **Either database is acceptable**, but a non-obvious data model must be
  explained rather than left implicit.
