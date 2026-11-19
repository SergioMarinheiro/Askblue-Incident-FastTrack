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
- **Manager:** reviews, edits, approves, rejects, sends back and marks duplicates. Sees all incidents of their company.
- **Admin:** everything a Manager can do, plus edit any field in any status and create users. Own company only.
- **SuperAdmin:** AskBlue platform staff with no company (`User.CompanyId` is null). Same powers as Admin, across all companies. It is the only role that bypasses tenant filtering.

### Statuses and transitions (`Enums/IncidentStatus.cs`)

| From | To | Who | Rule |
|---|---|---|---|
| (create) | New | Staff | Draft. Can be saved many times while partly filled. Staff choose the `ResolutionType`. |
| New, SentBack | AwaitingApproval | Staff | Staff submit it explicitly. Saving never moves the status. |
| AwaitingApproval | SentBack | Manager, Admin | Returned to Staff for changes, then resubmitted. |
| AwaitingApproval | Rejected | Manager, Admin | A reason is required (`Incident.RejectionReason`). Final. |
| AwaitingApproval | Duplicate | Manager, Admin | Final. |
| AwaitingApproval | AwaitingPayment | Manager, Admin (approve) | Automatic when `ResolutionType.RequiresPayment` is true (e.g. Compensation). |
| AwaitingApproval | AwaitingResolution | Manager, Admin (approve) | Automatic when `RequiresPayment` is false (e.g. HR, Legal, Insurance). |
| AwaitingPayment, AwaitingResolution | Completed | Staff, Manager, Admin | Sets `ClosedAt` and `ClosedByUserId`. |
| any open status | Canceled | Staff (own), Manager, Admin | Open = New, AwaitingApproval, SentBack, AwaitingPayment, AwaitingResolution. Never from Completed, Rejected or Duplicate. |

When approving, the Manager does not choose the next status. It always follows from `RequiresPayment`.

### Editing

- **Staff:** can edit their own incidents only while they are New or SentBack.
- **Manager:** can edit all fields until the incident is approved, rejected or marked Duplicate. A Manager may change the `ResolutionType` but doesn't have to.
- **Admin, SuperAdmin:** can edit any field in any status.

### Deletion

- Incidents are **never deleted**, by anyone, not even soft-deleted. Use the Canceled status to take an incident out of the active flow.

### Technical decisions

- JWT authentication, with login by email.
- Schema managed with EF Core migrations, not the SQL scripts in `database/migrations`.
- The tenant comes from the authenticated user's JWT claims.

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
