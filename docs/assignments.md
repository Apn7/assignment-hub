# Assignments

Teacher authoring with a draft/publish workflow, a student read side scoped to
the student's own class, and an admin read side over everything.

Submissions and grading are **not** part of this feature; a few rules below
anticipate them and say so.

> Rolls up into the README's *Features*, *Assumptions* and *Known Limitations*
> sections when that is written. See [auth.md](auth.md) for how the caller's
> identity and role are established.

## Endpoints

| Method | Route | Role | Purpose |
| --- | --- | --- | --- |
| `POST` | `/api/assignments` | `Teacher` | Create, always as a draft |
| `PUT` | `/api/assignments/{id}` | `Teacher` | Update own assignment |
| `POST` | `/api/assignments/{id}/publish` | `Teacher` | Draft → Published |
| `DELETE` | `/api/assignments/{id}` | `Teacher` | Delete own **draft** |
| `GET` | `/api/assignments/mine` | `Teacher` | Own assignments, both statuses |
| `GET` | `/api/assignments` | `Student` | Published assignments for own class |
| `GET` | `/api/assignments/{id}` | `Student` | Detail, if visible to their class |
| `GET` | `/api/admin/assignments` | `Admin` | Every assignment |

`mine`, `/admin/assignments` and the three teacher listings accept optional
`classRoomId`, `subjectId` and `status` query parameters. Each is an independent
narrowing filter; omitting one places no restriction on that dimension. A filter
can never widen what a caller is allowed to see, which is why teachers and admins
share the type.

The student list takes **no** class parameter. The class is read from the
student's stored record on every request, so there is nothing in the request to
tamper with, and an admin moving a student between classes takes effect without a
fresh login. Class membership is deliberately kept out of the token for the same
reason — a token claim would go stale for up to an hour.

## Status codes

One rule per code, applied everywhere. The mapping lives in exactly one place,
`ApiControllerBase.ErrorResponse`, so no individual action has to remember it.

| Code | Means |
| --- | --- |
| `400` | The request is wrong on its own terms — empty title, marks ≤ 0. State-independent. |
| `401` | No usable token. |
| `403` | The caller has no standing over this class/subject at all, or holds the wrong role. |
| `404` | Absent, or not the caller's to see. |
| `409` | Well-formed, but the assignment's current state forbids the transition. |

### Why 403 on create but 404 on update

`POST` returns **403** when a teacher is not assigned to the class and subject
they named. They chose those two ids themselves out of admin-managed lists, so
naming them back tells them nothing they did not already have, and a specific
message is the difference between a usable error and a dead end.

`PUT`, `POST .../publish` and `DELETE` return **404** for another teacher's
assignment. Here the id *is* the secret: a 403 would confirm that the id names a
real assignment, letting anyone with a Teacher token map out what their
colleagues have created by watching 403 and 404 alternate. So "no such
assignment" and "not yours" are one outcome, with one message — the same
reasoning that makes wrong-email and wrong-password indistinguishable at login.

The service enforces this structurally rather than by convention: every
not-found path returns the single `NotFoundMessage` constant, and
`NotFoundOutcomes_AllCarryTheSameMessage` asserts that four different reasons
produce one identical string. The two cases *are* distinguished in the server
log, where "not yours" is a warning and "does not exist" is information.

## Business rules

Every rule lives in `AssignmentService`, so none of them can be bypassed by
reaching a different endpoint, and each has at least one unit test.

| # | Rule | Rejected with |
| --- | --- | --- |
| 1 | A teacher may only create work for a `(class, subject)` pair they hold a `TeacherAssignment` for | `403` |
| 2 | A teacher may only update, publish or delete their own assignments | `404` |
| 3 | Maximum marks must be between 1 and 1000 | `400` |
| 4 | Publishing requires a deadline strictly in the future | `409` |
| 5 | Once published: title and description stay editable, the deadline may only move later, and class, subject and maximum marks are frozen | `409` |
| 6 | Published never returns to draft | not expressible |
| 7 | Only a draft may be deleted | `409` |
| 8 | Drafts are invisible to students everywhere | `404` |

**Rule 1 is checked twice.** Also on update, whenever a draft's class or subject
changes. Without that, creating a legitimate draft and then re-pointing it would
walk straight around the create-time check.

**Rule 3 is enforced twice, deliberately.** The FluentValidation rule gives the
client a field-level `400` at the edge; the service check keeps the rule true for
any caller. Both read the same constants from `AssignmentRules`, so they cannot
drift.

**Rule 4 is strict.** A deadline of exactly "now" is rejected: publishing work
that is already due gives students no time at all. The rejection message says to
extend the deadline first, and `PublishAsync_AfterTheDeadlineIsExtended_Succeeds`
proves that advice actually works.

**Rule 5 compares against stored values.** `PUT` is a full representation, not a
patch, so an edit form that round-trips the current class, subject and marks
unchanged is always accepted — only an actual difference is a violation. A
published assignment is something a class has already seen and that marks will
later be interpreted against, which is why the fields that change its meaning are
frozen while the wording is not.

**Rule 6 is enforced by construction, not by a guard.** `UpdateAssignmentRequest`
has no status field, and `UpdateAsync` never assigns to `Status`; the only status
write in the service is `Draft → Published` inside `PublishAsync`. There is
therefore no code path to guard — adding one would mean deliberately adding a
field, which is the point. Two tests hold the line: `UpdateRequest_HasNoStatusField`
on the shape, and `UpdateAsync_OnAPublishedAssignment_LeavesItPublished` on the
behaviour. Withdrawing published work is a different operation from editing it
and is out of scope.

**Rule 7 anticipates submissions.** A draft cannot have submissions, because
students never see drafts, so today the rule costs nothing. Once submissions
exist, deleting a published assignment would either orphan or cascade them; the
foreign keys are `Restrict`, so it would fail at the database instead. Rejecting
it here means a clear `409` rather than a `500` later.

**Rule 8 is enforced by the database.** `AssignmentQueries.VisibleToStudent` is a
single `Expression` — published, and this class — that both the list query and
the by-id query pass to EF Core, so it becomes part of the SQL `WHERE` clause. A
draft is never loaded, rather than loaded and then filtered out, and the two
student endpoints cannot disagree about what is visible. The unit tests compile
that same expression, so the rule under test is the rule that runs.

### Publishing twice is a conflict, not a no-op

`POST /publish` on an already-published assignment returns **409**, not 200.

Publishing is an event rather than a state to converge on: it is the moment work
becomes visible to a class, and it is where a notification would fire if
notifications are ever added. A silent second success would hide a
double-submitting client at exactly the point where that matters most. The cost
is that a client which loses the response must read before retrying — acceptable
for a deliberate, low-frequency teacher action.

## Design notes

**Services report failure as data.** `Result` / `Result<T>` carry a
`ResultStatus`, and the controller maps it. Exceptions would push the
403-versus-404 decision into middleware far from the rule that motivates it, and
would turn every rule test into an assertion about a thrown type instead of a
returned value.

**Controllers are one line each.** The role gate is the `[Authorize]` attribute;
every other decision belongs to `IAssignmentService`, which takes the acting
user's id as a parameter rather than reading an ambient context. That keeps
`HttpContext` out of the Application layer and makes "acting as teacher2 on
teacher1's assignment" a one-line test.

**A draft may hold a past deadline.** Nothing rejects it at creation, because
rule 4 is a publish-time rule: drafting next term's work with a placeholder date
is legitimate, and the check that matters happens when students would first see
it.

**`POST` returns 201 with no `Location` header.** The only by-id endpoint is the
student view, so a `Location` would point the creating teacher at a route that
would refuse them. Omitting it beats pointing somewhere untrue.

**A deadline without a timezone is read as UTC.** `"2026-09-01T10:00:00Z"`
arrives as UTC; `"2026-09-01T10:00:00"` arrives as `Unspecified` and is labelled
UTC — the same reading the persistence layer's `UtcDateTimeConverter` applies, so
the value the clock is compared against and the value stored always agree. Npgsql
rejects a non-UTC `DateTime` outright, which makes this a correctness matter
rather than a nicety.

**Responses carry names, not just ids.** `AssignmentResponse` includes the class,
subject and teacher names so a client can render a list without a request per
row. All three come from one query with three `Include`s.

**Reads are untracked; the write path re-reads.** Every list and detail query is
`AsNoTracking()`. `GetForUpdateAsync` is the one tracked query, and it loads no
navigations because the caller only reads and writes scalar fields. After a write
the service re-reads through `GetDetailAsync`, so the response reflects what the
database now holds rather than what the service believes it wrote.

## Verified behaviour

Checked by hand against Swagger and `curl` on the seeded Development database.

| Case | Result |
| --- | --- |
| teacher1 creates for Class 9-A / Physics (a pair they hold) | `201`, status `Draft` |
| teacher1 creates for Class 10-A / English (a pair they do not hold) | `403` |
| teacher1 edits their own draft | `200`, changes applied |
| teacher1 publishes the draft | `200`, status `Published` |
| teacher1 publishes it again | `409` |
| Class 9-A student lists assignments | `200`, the new assignment present |
| Class 10-A student lists assignments | `200`, the new assignment absent |
| Class 9-A student requests a draft by id | `404` |
| Class 10-A student requests the Class 9-A assignment by id | `404` |
| teacher2 edits, publishes or deletes teacher1's assignment | `404`, identical body each time |
| teacher1 deletes their own draft | `204` |
| teacher1 deletes a published assignment | `409` |
| teacher1 moves a published assignment to another class | `409` |
| teacher1 shortens a published deadline | `409` |
| teacher1 extends a published deadline | `200` |
| `maxMarks: 0` | `400` with field-level `errors` |
| Admin lists all assignments | `200`, every teacher's work including drafts |
| Admin filters by `classRoomId`, `subjectId`, `status` | `200`, narrowed correctly |
| Student calls a teacher route, or teacher calls the admin route | `403` |
| Any route with no token | `401` |
| Seeded draft / open / closed assignments after these rules exist | unchanged and consistent |

## Known limitations

- **No pagination.** Every listing returns the whole result set. Fine at seed
  scale and listed as optional in the requirements; the filters are the
  interim answer. Adding it later changes the response shape, so it is a
  breaking change deferred on purpose rather than forgotten.
- **Revoking a teacher assignment does not reach existing assignments.** If an
  admin removes a teacher's `TeacherAssignment`, that teacher can still edit,
  publish and delete assignments they already created for the pair, because
  those paths check ownership rather than current entitlement. The requirements
  do not say what should happen; documented rather than guessed at.
- **No withdraw or unpublish.** Rule 6 closes the door and nothing reopens it.
  A published assignment with a mistake can be reworded and its deadline
  extended, but not hidden again.
- **No teacher or admin detail-by-id endpoint.** Both roles get their detail from
  their listing. Only the student view needs by-id access, because only students
  navigate to an assignment they were linked to.
- **Repository queries are tested on EF Core's in-memory provider**, which runs
  LINQ against objects. That proves the queries are composed correctly —
  filters, ordering, scoping and `Include`s — but not that Npgsql translates
  them. The Postgres side is covered by the manual run recorded above.
- **No optimistic concurrency.** Two teachers cannot edit the same assignment
  (ownership is exclusive), but a teacher with two tabs open has last-write-wins.
  A `xmin` row version would be the fix.
