# AskBlue Incident FastTrack

Multi-tenant workplace incident management platform built by AskBlue and sold to multiple client companies (e.g. Sonae). Employees report workplace incidents; managers review, edit, and approve or reject them.

## Business workflow (confirmed requirements)

1. An employee submits an incident report (example: a customer fell because an item was stored improperly, and was injured).
2. A manager reviews the report.
3. The manager may edit the incident information.
4. The manager approves or rejects the incident.
5. An approved incident may lead to monetary compensation, or to escalation/referral to HR, Legal or Insurance.

Out of scope: contacting the affected customer. Do not build it.

Anything not listed here is not yet a requirement. If a feature seems to need a requirement that isn't stated (statuses, compensation rules, who can see what), ask me. Don't invent one.

## Confirmed requirements (2026-11-19)

### Roles (`Enums/UserRole.cs`)

- **Staff:** reports incidents. Sees only their own incidents.
- **Manager:** reviews, edits, approves, rejects, sends back, and dismisses or confirms duplicate flags. Can list and view users, and deactivate or reactivate Staff. Sees all incidents of their company.
- **Admin:** everything a Manager can do, plus edit any field in any status, manage users (create, list, get, edit, deactivate) and create stores and locations. Own company only.
- **SuperAdmin:** AskBlue platform staff with no company (`User.CompanyId` is null). Same powers as Admin, across all companies. It is the only role that bypasses tenant filtering.

### Statuses and transitions (`Enums/IncidentStatus.cs`)

| From | To | Who | Rule |
|---|---|---|---|
| (create) | New | Staff | Draft. Can be saved many times while partly filled. Only `CustomerName` is required to create it. To submit it, every field is required except `EstimatedCompensationAmount`, which is always optional. Staff choose the `ResolutionType`. |
| New, SentBack | AwaitingApproval | Staff | Staff submit it explicitly. Saving never moves the status. |
| AwaitingApproval | SentBack | Manager, Admin | Returned to Staff for changes, then resubmitted. What to fix is explained in a comment. |
| AwaitingApproval | Rejected | Manager, Admin | A reason is required (`Incident.RejectionReason`). Final. |
| AwaitingApproval | Duplicate | Manager, Admin | Only when the incident has a pending duplicate flag (see Duplicates). An explanation is given in a comment. Final. Sets `ClosedAt`/`ClosedByUserId`. |
| AwaitingApproval | AwaitingPayment | Manager, Admin (approve) | Automatic when `ResolutionType.RequiresPayment` is true (e.g. Compensation). |
| AwaitingApproval | AwaitingResolution | Manager, Admin (approve) | Automatic when `RequiresPayment` is false (e.g. HR, Legal, Insurance). |
| AwaitingPayment, AwaitingResolution | Completed | Staff, Manager, Admin | Sets `ClosedAt` and `ClosedByUserId`. |
| any open status | Canceled | Staff (own), Manager, Admin | Open = New, AwaitingApproval, SentBack, AwaitingPayment, AwaitingResolution. Never from Completed, Rejected or Duplicate. |

When approving, the Manager does not choose the next status. It always follows from `RequiresPayment`.

### Editing

- **Staff:** can edit their own incidents only while they are New or SentBack.
- **Manager:** can edit all fields until the incident is approved, rejected or marked Duplicate. A Manager may change the `ResolutionType` but doesn't have to.
- **Admin, SuperAdmin:** can edit any field in any status.
- **History:** every edit is recorded in `IncidentHistory` (field, old value, new value, who, when), whoever makes it and whatever the status, including draft edits. Fields saved together share one `ChangeSetId`.

### Duplicates (`Models/IncidentDuplicate.cs`, `Enums/DuplicateReviewStatus.cs`)

1. When Staff submit an incident (New/SentBack → AwaitingApproval), the system checks for possible duplicates among **open** incidents of the same company.
   - **Scoring:** each matching criterion adds a weight: same incident type, same store, same location, same reporter (`CreatedByUserId`). If the total reaches a threshold, the other incident is a possible duplicate. *(Exact weights and threshold: not decided yet.)*
2. API flow:
   - `POST /api/incidents/{id}/submit` runs the check.
     - **No matches:** AwaitingApproval (200).
     - **Matches:** the status doesn't change, and the response is 409 Conflict with the list of possible duplicates.
   - Staff then choose one of:
     - **Proceed:** `POST /api/incidents/{id}/submit` with `{ "proceedDespiteDuplicates": true }`. The incident goes to AwaitingApproval with `IncidentDuplicate` rows linking it to each match (`ReviewStatus = Pending`).
     - **Acknowledge it's a duplicate:** `DELETE /api/incidents/{id}` **hard-deletes** the incident. This is the only case where an incident is ever deleted. It's allowed only for the incident's own Staff, only while it is New or SentBack, and only right after a duplicate check found matches.
3. The Manager sees the flag and the matching incidents, then either:
   - **Dismisses the flag:** `ReviewStatus = NotDuplicate`, and the normal flow continues.
   - **Confirms it:** `ReviewStatus = ConfirmedDuplicate`, status becomes Duplicate, the incident is closed, and the explanation is written as a comment.

### Comments and attachments

- Both are in scope. They are general incident features, not part of duplicate detection.
- **Comments:** anyone who can see an incident can add a comment to it. Editing and deleting comments is out of scope for now, to be revisited once the backend and database are done.
- **Attachments:** where files are stored, the size limit and the allowed types are not decided yet. Ask before implementing.

### Stores and locations

- Admin (own company) and SuperAdmin can create stores and the locations inside them. Staff choose a store and location when reporting an incident.

### Users

- Admin (own company) and SuperAdmin can create, list, get and edit users.
- **On create**, the creator sets the initial password.
- **Roles a creator may assign:**
  - Admin: Staff, Manager or Admin, own company only.
  - SuperAdmin: any role, including SuperAdmin. Only a SuperAdmin can create a SuperAdmin.
- Managers can list and view the users of their own company.
- **Deactivate and reactivate** (`IsActive`):
  - Manager: only Staff of their own company.
  - Admin: any user of their own company.
  - SuperAdmin: any user.
- Users are never deleted. Deactivation is enough.

### Resolution types

- **Per company** (`ResolutionType.CompanyId`): each company has its own list, and other companies never see it.
- Created and edited by that company's Admin, or by SuperAdmin. Staff and Manager only read them, to pick one on an incident.

### Deletion

- Incidents are never deleted, not even soft-deleted. Use the Canceled status to take an incident out of the active flow.
- **The only exception** is the duplicate acknowledgement at submit (see Duplicates), which hard-deletes the incident through `DELETE /api/incidents/{id}`.

### Technical decisions

- JWT authentication, with login by email.
- Schema managed with EF Core migrations, not the SQL scripts in `database/migrations`.
- The tenant comes from the authenticated user's JWT claims.
- **SuperAdmin exception:** request DTOs that create company-owned data (users, stores, resolution types) may carry an optional `CompanyId`. It is accepted only from a SuperAdmin. From any other role it is rejected with 403, never silently used.
- **Errors:**
  - Services throw `NotFoundException`, `ForbiddenException` or `BusinessRuleException`.
  - One central exception handler turns them into ProblemDetails responses: 404, 403, and 409/422 respectively.
  - Controllers don't catch them.

### Not yet decided

Open questions live in `docs/architecture-review.md`, section E.

## Stack

- C#, ASP.NET Core Web API, .NET 10
- SQL Server, Entity Framework Core
- Docker Compose for local development
- Swagger/OpenAPI
- Backend project: `backend/ApiIncidentFastTrack`

## Multi-tenancy (critical)

- Every incident and user belongs to exactly one client company (tenant).
- A user must never read or change data belonging to another company. This is a security requirement, not a nice-to-have.
- Tenant isolation must be enforced on the server side (e.g. tenant taken from the authenticated user's claims), never trusted from a request body, query string or route value supplied by the client.
- Any new query, endpoint or service that touches tenant-owned data must apply tenant filtering. Point out any place where it doesn't.
- Roles: at least Employee and Manager. A manager acts only within their own company.

## Authentication

Being built as a separate concern. Known components: `IAuthService`, `AuthService`, `AuthController`, login DTOs. Login was previously a placeholder, so check the current code before assuming its state.

## Engineering standards

- Keep it simple and maintainable. Use clean architecture and SOLID where they give real value, and skip abstractions and patterns that don't.
- Keep controllers thin, put business logic in services, and use constructor-injected dependencies.
- Use DTOs at the API boundary. Never return EF entities directly.
- Validate all input, and return meaningful errors with correct HTTP status codes (ProblemDetails where it fits the existing code).
- Use async/await end to end and pass `CancellationToken` through where the existing code does.
- Follow the naming and folder conventions already in the repo.
- Write unit tests for business logic. If there is no test project yet, propose one (framework and packages) and wait for my approval before creating it.

## Working rules

- This is an existing project. Read the relevant code before proposing changes, and never assume a class, interface, service or config is missing without searching for it first.
- Don't rewrite working code to match your preferences. Respect existing decisions unless there's a concrete problem, and if there is one, explain it and ask.
- Make one focused, incremental change at a time, and don't touch unrelated files.
- Before adding a NuGet package, explain why it's needed and what the alternative would be, and wait for approval.
- Before changing Docker Compose, connection strings, or database configuration, explain the impact and wait for approval.
- When a change needs an EF migration, generate it but don't apply it to the database or edit existing migrations without asking.
- Never put secrets, connection strings, tokens or passwords in code, logs, commits or API responses. Never print the contents of secret files.
- Never commit, push, or create branches unless I explicitly ask.
- After making changes, run the build and the relevant tests and report the real results. Never claim something passes without running it.
- If an architectural decision could significantly affect the project, stop and ask.
- I'm using this project to improve my .NET skills: explain the important decisions and the reasons behind them, not just the code.
