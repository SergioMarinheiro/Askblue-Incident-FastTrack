# Architecture Review: ApiIncidentFastTrack

Date: 2026-11-19 · Branch: `feature/Incident` (HEAD `1dc6f6d`, plus uncommitted changes) · Scope: `backend/`

Labels: **[Existing]** = seen in the code (file cited) · **[Requirement]** = from `backend/CLAUDE.md` · **[Recommendation]** = my opinion.

## A. Current architecture

- [Existing] The solution `backend/IncidentFastTrack.slnx` has 2 projects: `ApiIncidentFastTrack` (Web API, net10.0) and `ApiIncidentFastTrack.Tests` (xUnit).
- [Existing] There is one project with folder-based layers: `Controllers/` → `Interfaces/` (service contracts) → `Services/`, with `DTOs/{Auth,Incidents}`, `Models/` (entities) and two enum folders (`Enums/`, `Domain/Enums/`).
- [Existing] The flow is Controller → `I*Service` (constructor-injected) → Service. There is no data access layer yet: no DbContext, no EF Core and no repository.
- [Existing] The only NuGet package is `Swashbuckle.AspNetCore` 10.1.0 (`ApiIncidentFastTrack.csproj`). EF Core, the SQL Server provider and the auth packages are not referenced.
- [Existing] `Models/*` and `Enums/*` are **untracked** (not committed) and **not referenced** by any controller, service or DTO.
- [Existing] `frontend/` is the default Vite + React template, and `database/{migrations,scripts}` contain only READMEs. Both are out of scope for this review.

## B. Implemented functionality (exists and appears to work)

Endpoints:

| Verb | Route | Controller | Auth | State |
|---|---|---|---|---|
| POST | `/api/auth/login` | `AuthController.cs:25` | none | [Existing] Returns a hard-coded fake token for any input |
| POST | `/api/auth/forgot-password` | `AuthController.cs:34` | none | [Existing] Returns a fixed string |
| POST | `/api/auth/reset-password` | `AuthController.cs:43` | none | [Existing] Returns a fixed string |
| POST | `/api/incidents` | `IncidentController.cs:18` | none | [Existing] Fails at runtime (see D) |
| GET | `/api/incidents` | `IncidentController.cs:30` | none | [Existing] Fails at runtime |
| GET | `/api/incidents/{id:int}` | `IncidentController.cs:38` | none | [Existing] Fails at runtime |
| DELETE | `/api/incidents/{id:int}` | `IncidentController.cs:49` | none | [Existing] Fails at runtime |

- [Existing] Controllers are thin, use `[ApiController]` and use constructor DI (`AuthController.cs`, `IncidentController.cs`).
- [Existing] `POST /api/incidents` returns `CreatedAtAction(GetIncidentById)` and the GET/DELETE endpoints return 404 when nothing is found. These are the correct status codes.
- [Existing] Swagger: `AddEndpointsApiExplorer` + `AddSwaggerGen`, with the UI only in Development (`Program.cs:11-12, 22-26`).
- [Existing] Entity model drafted in `Models/`: `Company`, `User`, `Store`, `Location`, `Incident`, `IncidentComment`, `IncidentAttachment`, `IncidentHistory` (field-level audit with `ChangeSetId`), `IncidentDuplicate`, `ResolutionType`. Relationships are expressed through navigation properties only. There is no Fluent configuration.
- [Existing] Launch profiles: http `localhost:5114`, https `localhost:7152` (`Properties/launchSettings.json`).

## C. Incomplete functionality

- [Existing] `IncidentService`: all 4 methods `throw new NotImplementedException()` (`Services/IncidentService.cs:10-25`).
- [Existing] `AuthService` is a placeholder. Login returns `"test-token"`, `Role = "Admin"`, and never checks credentials. Forgot and reset password return fixed strings (`Services/AuthService.cs`).
- [Existing] No `AddAuthentication` and no JWT/cookie scheme. `UseAuthorization()` is called, but no `[Authorize]` exists anywhere (`Program.cs:30`).
- [Existing] No DbContext, no EF Core configuration, no migrations. The DbContext registration in `Program.cs:15-18` is commented out, and it references `AppDbContext`/`CampanhaService` and a "Campanhas" message that look copied from another project.
- [Existing] No Docker Compose, Dockerfile or `.env` anywhere in the repo.
- [Existing] No connection string. `appsettings.json` only has `Logging:LogLevel:*` and `AllowedHosts`. `appsettings.Development.json` only has `Logging:LogLevel:*`. The csproj has no `UserSecretsId`, so user-secrets are not set up.
- [Existing] No input validation: no DataAnnotations, no FluentValidation, no manual checks in any DTO.
- [Existing] No ProblemDetails (`AddProblemDetails`/exception handler not registered).
- [Existing] No `CancellationToken` in any controller or service signature.
- [Existing] There are no endpoints for update/edit, approve/reject, compensation/referral, users, companies or stores.
- [Requirement] Missing for the core workflow: manager edit, approve/reject, and compensation or HR/Legal/Insurance referral.
- [Requirement] The Docker Compose + SQL Server local setup is not present.

## D. Concerns

### D1. Bugs
- **Resolved (task 14):** all services are now registered in DI.
- [Existing] **`IIncidentService` is not registered in DI** (`Program.cs:7` registers only `IAuthService`). Every `/api/incidents` request fails with a 500 (`InvalidOperationException` when the controller is activated). Even if it were registered, the service would then throw `NotImplementedException`.
- [Existing] **The test targets a deleted endpoint.** `HealthControllerTests.cs:20` calls `GET /api/health`, but `HealthController.cs` was deleted in commit `5770bda`. `ApiIncidentFastTrack.http:3` also still calls `/api/health`. **Confirmed:** the test fails with `Expected: OK, Actual: NotFound`.
  - **Resolved (task 2):** the health endpoint was template scaffolding, not part of the project. The test and the `.http` request were removed.
- [Existing] `WebApplicationFactory<Program>` compiles under .NET 10 with top-level statements. **Confirmed** by the build, so no `public partial class Program` is needed.

### D2. Inconsistencies
- [Existing] **Two incident-status enums:**
  - `Enums/IncidentStatus.cs` is the one in use. Confirmed set: New, AwaitingApproval, AwaitingPayment, AwaitingResolution, Completed, Rejected, SentBack, Canceled, Duplicate.
  - `Domain/Enums/IncidentStatus.cs` (old namespace) still has New, AwaitingApproval, AwaitingPayment, SentBack, Canceled, Rejected, Complete. It should be removed.
  - The deleted design docs had a third set: Submitted, Triage, Accepted, and so on.
- [Existing] **Roles:** `Enums/UserRole.cs` = SuperAdmin, Admin, Manager, Staff (meanings confirmed in E). `AuthService` hard-codes `"Admin"`. The deleted README used User/Owner/Admin.
- [Existing] The model has no dedicated approval or rejection fields: no approver, approval date or rejection reason. It also has no status that matches `ClosedAt`/`ClosedByUserId`.
- **Resolved:** `Domain/Enums/` was deleted by the user. It was not part of the project. The note below is kept for history.
- [Existing] **Two enum folders, two namespaces:**
  - `Domain/Enums/*` use `IncidentFastTrack.Api.Domain.Enums`, the old project name. `Enums/*` use `ApiIncidentFastTrack.Enums`.
  - The test namespace is still `IncidentFastTrack.Api.Tests`.
  - Two `IncidentStatus` types exist, so importing both namespaces makes the name ambiguous.
- [Existing] `ExternalPartyType` (Insurer, HR, Billing, Legal, Other) vs [Requirement] HR, Legal, Insurance.
- [Existing] DTOs and entities don't match:
  - `CreateIncidentRequestDto.Priority` is a `string`, but the entity uses the `IncidentPriority` enum.
  - The DTO is missing required entity fields: StoreId, LocationId, IncidentType, IncidentDate, CustomerName.
- [Existing] Login identity doesn't match: `LoginRequestDto` uses `Name` + `Password`, `User` has `Email` (unique in the design ERD), and `ForgotPasswordRequestDto` uses `Email`.
- [Existing] Mixed time types: `IncidentAttachment.UploadedAt` is still `DateTimeOffset`. Every other model uses `DateTime`.
- [Existing] Unused usings: `Microsoft.Extensions.Options` (`Program.cs:3`) and `ApiIncidentFastTrack.Services` (`AuthController.cs:4`).
- [Existing] `DELETE /api/incidents/{id}` is present, but **incidents must never be deleted** (confirmed 2026-11-19). The endpoint and `DeleteIncidentAsync` are to be removed (task 13).
- [Existing] The five root design files (`README.md`, `Incident-FastTrack-Design.docx`, `-Diagrama.png/.svg`, `-ERD.md`) are **deleted in the working tree but not committed**. Possibly accidental, possibly a OneDrive sync issue.

### D3. Security (non-tenant)
- [Existing] Every endpoint is anonymous, and login gives anyone a "token" with role Admin (`AuthService.cs:12-14`). The token is not checked anywhere, so it currently grants nothing. It still must not ship.
- [Existing] `Incident` stores customer PII (`CustomerName/Address/Email/Phone`). Nothing defines who may see it or how long it is kept.
- [Recommendation] Swagger has no bearer security definition yet. Add one once auth exists.
- [Existing] **Vulnerable transitive package:** the build reports `NU1903`, a high-severity advisory for `Microsoft.OpenApi` 2.3.0 ([GHSA-v5pm-xwqc-g5wc](https://github.com/advisories/GHSA-v5pm-xwqc-g5wc)). It comes in through `Swashbuckle.AspNetCore` 10.1.0. Swagger UI is Development-only (`Program.cs:22-26`), but the package is still in the build.
  - [Recommendation] Check for a patched Swashbuckle or `Microsoft.OpenApi` version, or pin a patched `Microsoft.OpenApi` directly. Needs package approval.

### D4. Multi-tenancy
- **How it is modelled today**
  - [Existing] `Company` is the tenant (`Models/Company.cs`).
  - [Existing] `CompanyId` exists on `Incident` (required), `Store` (required) and `User` (**nullable**, `Models/User.cs:9`).
  - [Existing] `Location` has **no `CompanyId`**. Its tenant only comes indirectly through `Store`.
  - [Existing] `ResolutionType` has **no `CompanyId`**, so it is global. Same for `IncidentComment`, `IncidentAttachment`, `IncidentHistory` and `IncidentDuplicate`, whose tenant only comes indirectly through `Incident`.
- **How it is enforced today**
  - [Existing] **It isn't.** There are no claims, no current-user/tenant accessor, no global query filter, no DbContext, and no tenant check in any service.
- **Where cross-tenant access looks possible**
  1. [Existing] `GET /api/incidents`: no tenant filter in the signature. `GetAllIncidentsAsync()` takes no tenant or user (`IIncidentService.cs:6`).
  2. [Existing] `GET /api/incidents/{id}`: looks up by id only (`IIncidentService.cs:7`). With sequential `int` ids, other companies' incidents could be enumerated.
  3. [Existing] `DELETE /api/incidents/{id}`: deletes by id only (`IIncidentService.cs:9`).
  4. [Existing] `POST /api/incidents`: nothing sets `CompanyId` yet. Good: the DTO doesn't accept it. Once StoreId/LocationId are added, the server must check that they belong to the caller's company.
  5. [Existing] The model doesn't stop a company mismatch. `Incident.CompanyId`, `Incident.StoreId` → `Store.CompanyId`, and `Incident.LocationId` → `Location.StoreId` → `Store.CompanyId` can disagree, so an incident in company A could reference company B's store or location.
  6. [Existing] `IncidentDuplicate` can link incidents from two different companies (`Models/IncidentDuplicate.cs`).
  7. [Existing] Several user links are never checked against the incident's company: `ClosedByUserId`, `ReviewedByUserId`, `UploadedByUserId`, `ChangedByUserId` and comment `UserId`.
  8. [Existing] `User.CompanyId` is nullable, which is intended for SuperAdmin (confirmed in E).
   - [Recommendation] Enforce it: CompanyId is required for every role except SuperAdmin.
   - [Recommendation] Treat SuperAdmin as an explicit, audited bypass of the tenant filter, never as an accidental one.
- [Requirement] The tenant must come from the authenticated user's claims, never from the request body, query string or route.
- [Recommendation] Use a scoped `ICurrentUser` (UserId, CompanyId, Role from claims) and an EF Core global query filter on `CompanyId`, plus explicit tenant checks in services for writes and foreign-key ownership. That covers both reads and writes.

## E. Open questions (for you to decide)

### Confirmed (2026-11-19)
- **Roles:**
  - `Staff` = employee who reports incidents.
  - `Manager` = reviews, edits, and decides incidents within their own company.
  - `Admin` = a client company's admin.
  - `SuperAdmin` = AskBlue platform staff with no company. This is why `User.CompanyId` is nullable.
- **Statuses** (`Enums/IncidentStatus.cs`): New, AwaitingApproval, AwaitingPayment, AwaitingResolution, Completed, Rejected, SentBack, Canceled, Duplicate. `Approved` was renamed to `Completed`.
- **Status flows:**
  1. New → AwaitingApproval → AwaitingPayment (if there is a payment) → Completed
  2. New → AwaitingApproval → AwaitingResolution (if HR, Legal or Insurance is selected) → Completed
  3. New → AwaitingApproval → Duplicate
  4. New → AwaitingApproval → … → Canceled
  5. New → AwaitingApproval → SentBack (Staff changes the incident) → AwaitingApproval → …
- **Who triggers transitions:**
  - SentBack and Duplicate: Manager.
  - Canceled: any user.
  - A SentBack incident is open for editing again and goes back to approval.
- **Visibility:**
  - Staff see only their own incidents.
  - Manager and Admin see all incidents of their company.
  - SuperAdmin sees everything.
- **Admin abilities:** create users within their own company.
- **Manager edits:** a Manager can edit all fields until the incident is approved, rejected or marked Duplicate.
- **Outcome (`ResolutionType`):**
  - Staff choose it when reporting the incident. A Manager may change it while editing, but doesn't have to.
  - Options include Compensation, HR, Legal and Insurance. `EstimatedCompensationAmount` holds the amount for Compensation.
- **Status after approval is automatic**, based on `ResolutionType`. The Manager only clicks approve.
  - Compensation → AwaitingPayment → Completed.
  - HR, Legal or Insurance → AwaitingResolution → Completed.
- **Technical:** JWT authentication, login by email, EF Core migrations (not the SQL scripts in `database/migrations`).
- **Rejected:** only a Manager can reject, only from AwaitingApproval, and a reason is required. Rejected is final.
- **New is a draft:**
  - Staff can save an incident as New several times, partially filled, while gathering information.
  - Staff move it to AwaitingApproval explicitly ("submit"). It does not move automatically on save.
- **Completed:** set by Staff or Manager. Setting it also sets `ClosedAt`/`ClosedByUserId`.
- **Canceled:** Staff can cancel their own incidents, and Manager/Admin can cancel any incident in their company. Only **open** incidents can be canceled (New, AwaitingApproval, SentBack, AwaitingPayment, AwaitingResolution), never Completed, Rejected or Duplicate.
- **Admin and SuperAdmin:**
  - Admin can do everything within their own company: approve, reject, send back, cancel, create users, and **edit any field in any status**, including Completed, Rejected and Duplicate.
  - SuperAdmin has the same powers as Admin, but in all companies. It is the only role that bypasses the tenant filter.
- **Staff edits:** allowed only while the incident is New or SentBack.
- **Deletion:** incidents are **never deleted**, not even soft-deleted. Canceled is used instead. This replaces the earlier "soft delete" answer.
- **Model additions:** `RejectionReason` on `Incident` and `RequiresPayment` (bool) on `ResolutionType`. Both done in task 1. There is no `IsDeleted`/`DeletedAt`.
- **Routing after approval:** if `ResolutionType.RequiresPayment` is true, the incident goes to AwaitingPayment, otherwise to AwaitingResolution. Compensation is seeded with `RequiresPayment = true`. New outcome types need no code change.

- **More scope confirmed:**
  - Comments, attachments and duplicate detection are all in scope now.
  - Admin can create stores and locations.
  - Users: create, list, get, edit and deactivate, never delete. Managers can also deactivate users.
  - Duplicate flow: see `backend/CLAUDE.md`, "Duplicates". Acknowledging a duplicate at submit is the only case of a hard delete.

### Still open
1. **Approval record:** should the approver and decision date get dedicated fields on `Incident`, or only be recorded in `IncidentHistory`? (The rejection reason is confirmed as a field on `Incident`.)
2. **Password reset:** is forgot/reset password in scope now? How is the email sent?
3. **ResolutionType details:** should it be one global list or per company (today it has no `CompanyId`)? Can an incident have more than one outcome?
4. **Customer PII:** which customer fields are needed, and who may see them?
5. **Enum storage:** currently int (the EF default), chosen in task 4. Change to string before applying the migration if you prefer readable values in SQL.

## F. Roadmap (ordered)

1. **Clean up the working tree and enums**
   - What: decide on the root doc deletions. Pick one enum folder and namespace (`ApiIncidentFastTrack.*`) and delete the other. Commit the `Models/` decision.
   - Why: removes ambiguity before the data layer is built on top of it.
   - Files: `Domain/Enums/*`, `Enums/*`, `Models/Incident.cs` (add `RejectionReason`), `Models/ResolutionType.cs` (add `RequiresPayment`).
   - Depends on: E1, E2, E16.
2. **Fix the broken test and the `.http` file**
   - What: remove the template health leftovers. There is no health endpoint in this project by decision.
   - Why: get a green baseline before building.
   - Files: `ApiIncidentFastTrack.Tests/HealthControllerTests.cs` (deleted), `ApiIncidentFastTrack.http` (request removed).
   - Depends on: none.
3. **Docker Compose + SQL Server + connection string (needs approval)**
   - What: `docker-compose.yml` with SQL Server, and the connection string through user-secrets or environment variables.
   - Why: CLAUDE.md requires it, and it is needed for EF.
   - New files: `docker-compose.yml`, possibly `.env.example`.
   - Files: csproj (`UserSecretsId`), `appsettings.Development.json` (key name only).
   - Depends on: E14.
4. **EF Core + DbContext + entity configuration + first migration (needs package approval)**
   - What: `Microsoft.EntityFrameworkCore.SqlServer` + `.Design`, `AppDbContext`, `IEntityTypeConfiguration<>` per entity (keys, required fields, string lengths, decimal precision, delete behaviour, enum conversion), and an initial migration. The migration is generated but not applied.
   - Why: persistence for everything else.
   - New files: `Data/AppDbContext.cs`, `Data/Configurations/*.cs`, `Migrations/*`.
   - Files: `Program.cs`, `Models/*` (trimmed to the confirmed scope).
   - Depends on: 1, 3, E10, E11, E15.
5. **Authentication (real login)**
   - What: hash passwords (e.g. ASP.NET Core `PasswordHasher<T>`, which is built in) and check them against the database. Issue a JWT with UserId, CompanyId and Role claims, plus `AddAuthentication`/`AddAuthorization` and the Swagger bearer definition.
   - Why: nothing can be protected or tenant-scoped without it.
   - Files: `AuthService.cs`, `IAuthService.cs`, DTOs, `Program.cs`, `appsettings.json` (non-secret settings).
   - Packages: `Microsoft.AspNetCore.Authentication.JwtBearer` (needs approval).
   - Depends on: 4, E4, E5.
6. **Tenant and current-user plumbing**
   - What: a scoped `ICurrentUser` read from claims, a global query filter on `CompanyId`, and `[Authorize]` with role policies on controllers.
   - Why: tenant isolation (security requirement).
   - New files: `Interfaces/ICurrentUser.cs`, `Services/CurrentUser.cs`.
   - Files: `AppDbContext.cs`, `Program.cs`, controllers.
   - Depends on: 5, E1, E3.
7. **Cross-cutting API hygiene**
   - What: `AddProblemDetails` + an exception handler, DTO validation (DataAnnotations; FluentValidation only if approved), and `CancellationToken` through controllers and services.
   - Why: CLAUDE.md standards. Errors currently leak as raw 500s.
   - Files: `Program.cs`, DTOs, interfaces, services.
   - Depends on: none (can run alongside 4–6).
8. **Incident create and read (tenant-scoped)**
   - What: implement `IncidentService`, register it in DI, align the DTOs with the entity, check Store/Location ownership, and map to response DTOs.
   - Why: the first workflow step.
   - Files: `IncidentService.cs`, `IIncidentService.cs`, `DTOs/Incidents/*`, `Program.cs`.
   - Depends on: 4, 6, 7, E7, E10.
9. **Manager edit with history**
   - What: `PUT`/`PATCH /api/incidents/{id}`, Manager only, writing to `IncidentHistory`.
   - Why: workflow step 3.
   - New files: `UpdateIncidentRequestDto.cs`.
   - Depends on: 8, E8.
10. **Approve/reject plus status transitions**
    - What: transition endpoints and a status state machine in the service.
    - Why: workflow step 4.
    - New files: approve/reject DTOs.
    - Depends on: 8, E2.
11. **Compensation / referral**
    - What: model and endpoints for the post-approval outcome.
    - Why: workflow step 5.
    - New files: entity, DTOs, migration.
    - Depends on: 10, E9.
12. **Unit tests for business logic**
    - What: tests for the `IncidentService` rules (tenant checks, status transitions, edit history). Use the existing xUnit project and propose any extra packages (e.g. EF InMemory/SQLite) for approval.
    - Why: CLAUDE.md requires them.
    - Files: `ApiIncidentFastTrack.Tests/*`.
    - Depends on: 8–11, written alongside each.
13. **Remove the DELETE endpoint**
    - What: remove `DELETE /api/incidents/{id}` and `DeleteIncidentAsync` (controller, interface, service). Decided: incidents are never deleted, only canceled.
    - Depends on: E12.

## Verification
Run on 2026-11-19 against the working tree, including the uncommitted `Models/` and `Enums/`.

- `dotnet build backend/IncidentFastTrack.slnx`: **Build succeeded, 0 errors, 4 warnings.**
  - All 4 warnings are `NU1903` for `Microsoft.OpenApi` 2.3.0 (see D3), reported once per project during restore and again during build.
- `dotnet test backend/IncidentFastTrack.slnx`: **Failed: 1, Passed: 0, Total: 1.**
  - `HealthControllerTests.GetHealth_ReturnsOkAndReportsStatus`: `Expected: OK, Actual: NotFound` (`HealthControllerTests.cs:22`). `/api/health` no longer exists (see D1).

## Task status
| # | Task | Status |
|---|---|---|
| 1 | Working tree + enums cleanup | **Partly done** (2026-11-19): `Domain/` deleted by user; added `Incident.RejectionReason`, `ResolutionType.RequiresPayment`; `IncidentAttachment.UploadedAt` → `DateTime`. Soft delete dropped (never delete). Old root design files confirmed obsolete and left deleted. **Done** |
| 2 | Fix broken test / `.http` | **Done** (2026-11-19): health test and request removed; build OK, 0 tests |
| 3 | Docker Compose + SQL Server | **Partly done** (2026-11-19): `UserSecretsId` added to the csproj. SQL Server already runs in the user's own compose project (`askblue-sqlserver`, outside the repo, port 14330). Pending: whether the repo gets its own compose file, database name and login, and the user setting the connection-string secret |
| 4 | EF Core + DbContext + migration | **Done, not applied** (2026-11-19): EF Core 10.0.15 (SqlServer + Design), local tool `dotnet-ef` 10.0.15 (`backend/dotnet-tools.json`), `Data/AppDbContext.cs`, 10 configurations in `Data/Configurations/`, migration `Data/Migrations/*_InitialCreate`. Enums stored as int. No seed data yet. **Migration not applied to the database** |
| 5 | Authentication | Not started |
| 6 | Tenant / current-user plumbing | Not started |
| 7 | API hygiene (ProblemDetails, validation, CancellationToken) | Not started |
| 8 | Incident create/read | Not started |
| 9 | Manager edit + history | Not started |
| 10 | Approve/reject + transitions | Not started |
| 11 | Compensation / referral | Not started |
| 12 | Unit tests | Not started |
| 13 | Remove DELETE endpoint | **Done** (2026-11-19): endpoint, interface method and service stub removed; build OK. *Note: a new, narrowly guarded `DELETE` comes back with the duplicate flow (acknowledge a duplicate at submit). See `backend/CLAUDE.md`, "Duplicates".* |
| 14 | API contract (DTOs, interfaces, stub services, controllers, DI) | **Done** (2026-11-19). 33 endpoints in Swagger; services are `NotImplementedException` stubs; DI fixed; `Incident` fields nullable for drafts; `ResolutionType.CompanyId` added. Attachments not included (storage undecided) |
