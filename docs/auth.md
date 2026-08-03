# Authentication & Authorization

JWT bearer tokens with role-based authorization. Users are created by the seeder
(and later by an admin) — there is no self-registration.

> Rolls up into the README's *Assumptions* and *Known Limitations* sections when
> that is written.

## Endpoints

| Method | Route | Auth | Purpose |
| --- | --- | --- | --- |
| `POST` | `/api/auth/login` | anonymous | Exchange email + password for a token |
| `GET` | `/api/auth/me` | any authenticated user | Caller's identity, read from token claims |
| `GET` | `/api/auth/admin-check` | `Admin` only | **Temporary.** Proves 403 works; delete when real admin endpoints land |

### Login

```http
POST /api/auth/login
{ "email": "admin@assignmenthub.local", "password": "Admin#1234" }
```

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAtUtc": "2026-08-03T16:55:18.6738964Z",
  "user": {
    "id": "10000000-0000-0000-0000-000000000001",
    "fullName": "System Administrator",
    "email": "admin@assignmenthub.local",
    "role": "Admin"
  }
}
```

Send it as `Authorization: Bearer <accessToken>`. In Swagger UI, click
**Authorize** and paste the token — the `Bearer ` prefix is added for you.

Credentials are in [database.md](database.md#demo-credentials).

## Token contents

Four claims, using short JWT-standard names:

| Claim | Value |
| --- | --- |
| `sub` | user id (GUID) |
| `email` | user's email |
| `role` | `Admin`, `Teacher` or `Student` |
| `name` | full name |
| `jti` | unique token id |

Signed with HMAC-SHA256. Issuer, audience, signing key and lifetime (default 60
minutes) all come from the `Jwt` configuration section — nothing is hardcoded.

## Design decisions

**`[Authorize(Roles = ...)]`, not policies.** With three flat roles and no
claim-combination logic, a policy per role would be indirection without benefit.
The attribute takes `nameof(UserRole.Admin)` rather than the string `"Admin"`, so
renaming the enum member is a compile error instead of a silent authorization
hole. Policies become worthwhile when resource-level rules arrive ("this teacher
owns this assignment"), and those will live in Application services regardless —
see below.

**Inbound claim mapping is switched off.** By default the JWT handler rewrites
`sub` into `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier`
and friends. `MapInboundClaims = false`, plus `NameClaimType`/`RoleClaimType` set
to our own names, means the claims read back are exactly the claims signed. If
those two ends ever disagree, every role check silently returns 403 — a
frustrating bug worth designing out.

**Failure is a single unnamed outcome.** `IAuthService.LoginAsync` returns
`LoginResponse?`, and `null` means "authentication failed" with no reason
attached. The signature *cannot* express "no such user" separately from "wrong
password", so no present or future caller can turn the endpoint into a
user-enumeration oracle. The controller maps `null` to one fixed 401 body.

**Unknown emails still cost a password verification.** Returning early when the
address doesn't exist would make the two failures distinguishable by response
time. `AuthService` verifies against a decoy hash instead, so both paths do the
same PBKDF2 work.

**Emails are normalised on login** (trimmed, lower-cased) because people
capitalise addresses inconsistently. Seeded emails are stored lower-case.

**One hashing policy.** The seeder and the login path both resolve
`IPasswordHasher`, implemented once over Identity's `PasswordHasher<User>`
(PBKDF2-HMAC-SHA256, salted, versioned format). A hash written by one is
therefore always verifiable by the other. Only Identity's *hasher* is used — no
Identity stores, schema, middleware or UI.

**Validation on login checks presence and format only.** Password complexity
rules belong on the path that sets a password; enforcing them at login would
reject legitimate older passwords and advertise the policy to anyone probing.

**Startup fails fast** if `Jwt:Secret` is missing or under 32 bytes, rather than
degrading to tokens that cannot be securely verified. Note this also applies to
`dotnet ef` commands, which boot the API host — see
[database.md](database.md#3-apply-migrations).

## Verified behaviour

| Case | Result |
| --- | --- |
| Login as Admin / Teacher / Student | 200, correct `role` in response |
| `/api/auth/me` with valid token | 200, correct identity from claims |
| `/api/auth/me` with no token | 401 + `WWW-Authenticate: Bearer` |
| `/api/auth/me` with signature replaced | 401 |
| Token with `role` edited `Student`→`Admin`, original signature | 401 |
| Token with header downgraded to `alg: none` | 401 |
| `admin-check` as Admin | 200 |
| `admin-check` as Teacher or Student | 403 |
| Unknown email vs wrong password | Identical 401 body (bar the per-request `traceId`) |
| Malformed login body | 400 with field-level `errors` |

## Known limitations

Deliberately out of scope for this project:

- **No refresh tokens.** A 60-minute access token is it; when it expires the user
  logs in again. Refresh tokens need rotation, reuse detection and server-side
  storage to be worth having, and half of that is worse than none.
- **No logout or token revocation.** Tokens are stateless, so a issued token stays
  valid until it expires. Revocation would need a `jti` blacklist checked on every
  request — the `jti` claim is already there for it.
- **No account lockout or rate limiting** on repeated failed logins, so the login
  endpoint is open to online password guessing. ASP.NET Core's built-in rate
  limiter would be the shortest path to fixing this.
- **No password reset** and no way for a user to change their own password.
- **No registration endpoint.** Users come from the seeder, and later from
  admin-managed user creation.
- **Case-insensitive login, case-sensitive storage.** Login lower-cases the email
  before lookup, but the unique index on `Users.Email` is case-sensitive. Nothing
  currently stops an admin creating `Admin@x.com` when `admin@x.com` exists, which
  would then be unreachable by login. The complete fix is a `citext` column or a
  unique index on `lower("Email")`; the interim measure is to normalise on write
  when admin user-creation is built.
- **Resource-level authorization is not implemented yet.** Role checks answer "is
  this a Teacher"; they cannot answer "is this teacher assigned to *this* class".
  Those rules will live in Application services where they are unit-testable, and
  are the substance of the next steps.
