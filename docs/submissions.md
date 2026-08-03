# Submissions

Students hand in answers and see their marks; teachers mark the work and can send
it back for revision; admins see everything. Together with
[assignments.md](assignments.md) this closes the product loop the brief describes.

> Rolls up into the README's *Features*, *Assumptions* and *Known Limitations*
> sections when that is written.

## Endpoints

| Method | Route | Role | Purpose |
| --- | --- | --- | --- |
| `POST` | `/api/assignments/{assignmentId}/submissions` | `Student` | Hand in an answer |
| `PUT` | `/api/assignments/{assignmentId}/submissions/mine` | `Student` | Revise own answer |
| `GET` | `/api/assignments/{assignmentId}/submissions/mine` | `Student` | Own answer, status, marks, feedback |
| `GET` | `/api/assignments/{assignmentId}/submissions` | `Teacher` | Marking list for own assignment |
| `GET` | `/api/submissions/{id}` | `Teacher` | One submission in full |
| `POST` | `/api/submissions/{id}/grade` | `Teacher` | Record marks and feedback |
| `POST` | `/api/submissions/{id}/status` | `Teacher` | Set the status, e.g. reopen |
| `GET` | `/api/admin/submissions` | `Admin` | Every submission |

The admin listing accepts optional `assignmentId`, `classRoomId` and `status`.
`classRoomId` reaches through the parent assignment, because a submission has no
class of its own.

**Student routes are nested and addressed as `mine`.** A student has exactly one
submission per assignment, so they never need an id — and a route that took one
would invite an ownership check that this one cannot forget: the student id is part
of the query, not a condition applied afterwards.

**Teacher routes on a single submission are flat.** A submission id is globally
unique, so requiring the assignment id as well would add a segment the server would
have to either verify or ignore, and both are worse than not asking. Ownership is
settled through the submission's own parent assignment.

## Status codes

The same discipline as assignments, with one addition.

| Code | Means |
| --- | --- |
| `400` | The request is wrong on its own terms — empty answer, over-long feedback. State-independent. |
| `401` | No usable token. |
| `403` | Wrong role for this route. |
| `404` | Absent, or not the caller's to see. |
| `409` | Well-formed, but the current state forbids it — deadline passed, already submitted, already reviewed. |
| `422` | Well-formed and permitted, but a value is out of range **for this resource** — 11 marks on an assignment worth 10. |

### Why marks out of range is 422 and not 400

The legal range for `marks` runs from zero to *that assignment's* `MaxMarks`. The
same body — `{"marks": 11}` — is perfectly valid against an assignment worth 20 and
invalid against one worth 10, so no request validator can judge it. That is exactly
the distinction 422 exists for: understood, well-formed, and unprocessable against
this particular target.

This is why `GradeSubmissionRequest.Marks` has **no** FluentValidation rule at all.
Adding a partial one — rejecting negatives at the edge — would mean `-1` returned
400 while `999` returned 422, for the same underlying reason and with no way for the
caller to see the logic. The whole range check lives in the service, and the message
names the actual maximum: *"Marks must be between 0 and 10 for this assignment."*

## Business rules

Every rule lives in `SubmissionService`, so none can be bypassed by reaching a
different endpoint, and each has at least one unit test.

| # | Rule | Rejected with |
| --- | --- | --- |
| 1 | Students submit only to published assignments of their own class | `404` |
| 2 | Nothing may be submitted or revised after the deadline | `409` |
| 3 | One submission per student per assignment | `409` |
| 4 | A student cannot revise reviewed work, even before the deadline | `409` |
| 5 | Teachers see and grade only submissions on their own assignments | `404` |
| 6 | Marks must be between 0 and the assignment's `MaxMarks`, inclusive | `422` |
| 7 | An answer is required and capped at 20,000 characters | `400` |

**Rule 1 reuses the assignment visibility predicate.** Submitting goes through
`AssignmentQueries.VisibleToStudent`, the same expression behind the student
assignment list, so a draft or another class's assignment is not merely refused —
it is never loaded. The class comes from the student's stored record on every
request, never from the request body.

**Rule 2's boundary is inclusive: `now == deadline` is on time.** A student who
makes the deadline to the instant has made it. Note this is deliberately the
*opposite* convention from publishing an assignment, which requires a deadline
strictly in the future. The two look inconsistent side by side and are not: there,
the point is that students need time to work, so a deadline of "now" is useless;
here, the point is that a deadline is a deadline, and being exactly on it is not
being late. Both are unit-tested at the boundary and one tick either side.

**Rule 3 is enforced twice, and the second time is the one that counts.** The
service checks for an existing submission first, because that produces a helpful
message for the ordinary case. But a student who double-clicks can get two requests
past that check, and only the database can settle a race — so the unique index on
`(AssignmentId, StudentId)` is the real authority. `SubmissionRepository.TryAddAsync`
catches SQLSTATE `23505` and returns `false`, which the service turns into the same
409. A lost race therefore reads as a duplicate, never as a 500.

That translation is the reason `TryAddAsync` returns a `bool` rather than letting
the driver exception escape: the Application layer must not know what Npgsql is, so
Infrastructure converts the constraint violation into a value on the way out.

**Rule 4 is a documented assumption, not a stated requirement.** The brief says a
student may update a submission "before the deadline, **if allowed**". We read the
escape clause as teacher control, and implement it as: reviewed work is frozen, and
the teacher reopens it with the status endpoint. The rejection message says exactly
that, so the student knows what to ask for.

Rule 4 is checked **before** rule 2. Both can be true at once — reviewed *and* past
the deadline — and "it has been graded, ask your teacher to reopen it" is the one a
student can act on. The deadline message would be a dead end.

**Rule 5 gives 404, never 403.** A submission id is otherwise a probe for whether a
colleague's class has handed something in. Every not-found path returns the shared
`NotFoundMessages.Submission` constant, and
`TeacherNotFoundOutcomes_ShareOneMessagePerResource` asserts that three different
reasons produce one identical string. The two cases are still distinguished in the
server log, where "someone else's" is a warning and "does not exist" is
information.

Note the messages are per **resource**, not global: a bad assignment id on
`/api/assignments/{id}/submissions` returns *"Assignment not found."* — the same
string `AssignmentService` returns for the same id — while a bad submission id
returns *"Submission not found."* Both constants live in `NotFoundMessages` so two
services cannot drift apart.

## Grading and reopening

**Re-grading is allowed and simply replaces the verdict.** Marking mistakes happen,
and a correction should not require a database edit. A re-grade overwrites marks and
feedback and moves `ReviewedAt` to the new instant.

**A grade is the whole verdict, not a patch of it.** Omitting `feedback` on a
re-grade clears any previous comment rather than silently keeping it. Sending a
mark with no comment is legitimate — `Feedback` is nullable — so "no feedback" has
to mean no feedback.

**A status change alone preserves marks, feedback and `ReviewedAt`.** This is the
decision most worth stating plainly. Reopening reviewed work for revision is not
withdrawing the mark: the work really was reviewed at that instant, and the student
should keep seeing the feedback that told them what to fix. A teacher who wants to
change the mark grades it again, which is one request away. So after a reopen a
submission can read `Submitted` while still carrying `8` and a comment — intended,
and covered by
`ChangeStatusAsync_ReopeningReviewedWork_PreservesMarksAndFeedback`.

**Setting the status it already has is a no-op success**, not a conflict. Unlike
publishing an assignment — an event, where a duplicate hides a client bug — this
endpoint *sets state*, and setting state to what it already is has no victim.

**`ReviewedAt` is set only by grading, never by a status change**, because it means
"when marks were recorded" and a status change records no marks.

**`UpdatedAt` tracks the student's edits only.** Neither grading nor a status change
touches it. A teacher marking work is not an edit of the answer, and `ReviewedAt` is
where that instant belongs.

## Design notes

**Enum-valued request fields accept names.** `JsonStringEnumConverter` is registered
globally, so `{"status": "Submitted"}` works as well as `{"status": 1}`. Responses
were already sending enum names — the DTOs project them with `ToString()` — so this
only changed what is accepted. `IsInEnum()` is still needed on the validator,
because a raw out-of-range number like `99` deserialises happily.

**Two response shapes, for one real reason.** `SubmissionResponse` carries
everything; `SubmissionListItem` drops `AnswerText` and `Feedback`. An answer may
run to twenty thousand characters, so a class of thirty would make a marking
overview megabytes long for two fields nobody reads at that zoom level. The full
text is one request away. A test asserts the two fields stay absent.

Both roles share `SubmissionResponse`. There is nothing in it a student may not see
about their own work, or the owning teacher about a submission on their assignment,
so a second projection would add a type without adding a rule.

**Responses carry the assignment's `MaxMarks` and `Deadline`.** A client renders
"8 / 10" and "due Friday" without a second request per row.

## Verified behaviour

Checked by hand against Swagger and a scripted pass on the seeded Development
database — 118 assertions, all green.

| Case | Result |
| --- | --- |
| **The full loop** | |
| student1 submits to a published Class 9-A assignment | `201`, status `Submitted`, marks null |
| student1 revises before the deadline | `200`, `submittedAt` unchanged, `updatedAt` moved |
| teacher1 lists submissions on their own assignment | `200`, student name shown, answer omitted |
| teacher1 opens it in full | `200`, answer present |
| teacher1 grades 8 / 10 with feedback | `200`, status `Reviewed`, `reviewedAt` set |
| student1 sees `Reviewed`, `8`, and the feedback | `200` |
| student1 tries to revise | `409`, message says to ask for a reopen |
| teacher1 reopens via the status endpoint | `200`, status `Submitted`, marks and feedback and `reviewedAt` intact |
| student1 revises | `200` |
| teacher1 re-grades 10 / 10 | `200`, `reviewedAt` moved |
| **Rules** | |
| student1 submits to the seeded draft | `404` |
| Class 10-A student submits to a Class 9-A assignment | `404` |
| student1 submits to an id that does not exist | `404`, body identical to the two above |
| student1 submits to the seeded past-deadline assignment | `409`, message names the deadline |
| student1 submits twice to the same assignment | `409` |
| 24 simultaneous submits from one student | exactly one `201`, 23 × `409`, **no 5xx** |
| — of those, requests reaching the unique index | 5 hit SQLSTATE `23505` and were mapped to `409` |
| grade 11 / 10 | `422`, message names the maximum |
| grade −1 | `422` |
| grade 0 and grade 10 | `200`, both bounds inclusive |
| feedback over 2,000 characters | `400` |
| answer empty or whitespace-only | `400` with field-level `errors` |
| answer at exactly 20,000 / over 20,000 characters | `201` / `400` |
| **Authorization** | |
| teacher2 lists, opens, grades or restatuses teacher1's submission | `404`, one identical body |
| teacher1 opens teacher2's Class 10-A submission | `404` |
| student2 reads or revises `mine` where only student1 answered | `404` |
| teacher submits an answer, or reads `mine` | `403` |
| student lists an assignment's submissions, grades, or restatuses | `403` |
| teacher or student calls the admin listing | `403` |
| admin grades | `403` |
| any route with no token | `401` |
| **Admin** | |
| admin lists everything | `200`, both classes, both teachers' assignments |
| admin filters by `assignmentId`, `classRoomId`, `status`, and all three | `200`, narrowed correctly |
| bad `status` value | `400` |
| **Seeded data** | |
| student1's seeded ungraded submission | still `Submitted`, marks null |
| the seeded graded submission | still `Reviewed`, `42 / 50` |
| student1 submits again to the seeded Kinematics assignment | `409` |

## Known limitations

- **No file uploads.** Answers are text. The brief says "submit an answer" and
  never mentions attachments; adding storage, virus scanning and signed download
  URLs would be a larger feature than the rest of this one.
- **No late submissions at all.** The deadline is hard: there is no grace period
  and no "accepted late" flag a teacher could set. Extending the assignment's
  deadline is the supported route, and it works — a published deadline may be moved
  later.
- **A reopened submission can read `Submitted` while carrying marks.** Deliberate,
  and explained above, but a client should present it as "previously marked 8/10,
  awaiting re-marking" rather than as an ordinary unmarked submission.
- **No audit trail on status changes.** Neither the reopen nor the identity of the
  teacher who did it is stored — only logged. `UpdatedAt` deliberately tracks the
  student's edits, so nothing on the row records when a teacher last touched it. A
  `SubmissionEvent` table would be the fix if the history ever matters.
- **No notifications.** Nothing tells a student their work has been marked, or a
  teacher that work has come in. `POST /publish` and `POST /grade` are the two
  points where that would hook in.
- **No pagination**, as with assignments. The filters are the interim answer.
- **No optimistic concurrency.** Two teachers cannot grade the same submission
  (only the owning teacher can), but one teacher with two tabs open has
  last-write-wins.
- **Repository queries are tested on EF Core's in-memory provider**, which proves
  composition rather than Npgsql translation — and which does **not** enforce
  unique indexes, so `TryAddAsync`'s duplicate path cannot be reached there. That
  path is covered at the service level through `FakeSubmissionRepository`, and
  against real Postgres by the 24-way concurrent submit recorded above.
