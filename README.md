# Access Request & Approval System

> A secure, minimal privileged-access request and approval system built on .NET 8.  
> It replaces permanent privileged accounts with temporary, audited, just-in-time access grants that automatically expire and revoke themselves.

---

## Architecture

```
                       ┌────────────────────────┐
                       │  WinForms Admin App    │
                       │ (Approvers & Admins)   │
                       └───────────┬────────────┘
                                   │ HTTP / JSON (JWT)
                                   ▼
┌─────────────────┐       ┌────────────────────────┐
│ Requester / User│──────▶│ ASP.NET Core Web API   │
│ (Swagger / API) │ HTTP  │ (JWT, RBAC, Auditing)  │
└─────────────────┘       └───────────┬────────────┘
                                      │
                                      ▼
                        ┌───────────────────────────┐
                        │ SQL Server (EF Core)      │
                        │ - Users, Roles, Resources │
                        │ - AccessRequests          │
                        │ - Immutable AuditLogs     │
                        └─────────────▲─────────────┘
                                      │ Direct DbContextFactory
                                      │ (Parallel Execution)
                       ┌──────────────┴─────────────┐
                       │ Windows Service Worker     │
                       │ (Auto-revokes expired      │
                       │  grants every 30s)         │
                       └────────────────────────────┘
```

---

## Tech Stack

- **Framework**: .NET 8 (C# 12)
- **Data Layer**: Entity Framework Core 8 with Microsoft SQL Server
- **Web API**: ASP.NET Core Web API with JWT Bearer Authentication & Swagger / OpenAPI
- **Background Worker**: .NET BackgroundService running as a Windows Service (`Microsoft.Extensions.Hosting.WindowsServices`)
- **Admin Client**: Windows Forms (WinForms) desktop application using non-blocking async `HttpClient`
- **Testing**: xUnit with EF Core In-Memory provider

---

## Solution Layout

- **`AccessRequestSystem.Data`**: Entities (`User`, `Role`, `Resource`, `AccessRequest`, `AuditLog`), `AppDbContext`, and database migrations.
- **`AccessRequestSystem.Api`**: REST API endpoints, JWT token issuance, permission policies, and global RFC 7807 `ProblemDetails` exception handling.
- **`AccessRequestSystem.Worker`**: Periodic background service that queries expired grants and revokes them in parallel using `IDbContextFactory`.
- **`AccessRequestSystem.Admin`**: Desktop administration application for viewing pending requests, approving/denying access, and inspecting audit trails.
- **`AccessRequestSystem.Tests`**: Automated unit tests for business logic, token hashing, self-approval prevention, duration validation, and expiry processing.

---

## Setup Steps from a Fresh Machine

### Prerequisites
1. **.NET 8 SDK** (v8.0 or newer)
2. **SQL Server Express** or **LocalDB** running locally.
3. Windows OS (for running the WinForms client and Windows Service).

### 1. Clone & Configure
```powershell
git clone https://github.com/<your-username>/Access-Request-Approval-System.git
cd Access-Request-Approval-System
```

*(Optional)* Configure your JWT signing key via User Secrets or environment variable:
```powershell
dotnet user-secrets set "Jwt:Key" "MySuperSecretKeyWithAtLeast32Characters2026!" --project AccessRequestSystem.Api
```
*(In Development mode, a built-in fallback key is provided for immediate out-of-the-box local execution).*

### 2. Apply Migrations & Seed Database
You can apply EF Core migrations explicitly:
```powershell
dotnet ef database update --project AccessRequestSystem.Data --startup-project AccessRequestSystem.Api
```
*(Alternatively, simply launching the Web API will automatically apply any pending migrations and seed default data on startup).*

### 3. Run the Applications
Open separate terminals for each component:

```powershell
# 1. Start the API (Swagger available at https://localhost:7241/swagger)
dotnet run --project AccessRequestSystem.Api

# 2. Start the Expiry Background Worker
dotnet run --project AccessRequestSystem.Worker

# 3. Start the WinForms Admin Application
dotnet run --project AccessRequestSystem.Admin
```

---

## Seeded Users

Every database migration pre-seeds 3 default accounts with password **`Password123!`**:

| Username | Role | Permissions |
|---|---|---|
| `admin` | **Admin** | Approve/deny requests, view full audit logs, manage resources |
| `approver` | **Approver** | View pending requests queue, approve/deny requests |
| `requester` | **Requester** | Submit access requests, view personal request history |

---

## API Endpoint Reference

### Authentication
- `POST /api/auth/login`: Authenticate with username and password; returns JWT token with user ID, username, and role claims.

### Resources
- `GET /api/resources`: List all requestable privileged assets (Authenticated users).

### Access Requests
- `POST /api/requests`: Submit a new request (`Requester` only; duration 1–480 minutes; starts as `Pending`).
- `GET /api/requests/mine`: Retrieve own submitted requests (`Requester` only).
- `GET /api/requests/pending`: Retrieve requests waiting for decision (`Approver`, `Admin`).
- `POST /api/requests/{id}/approve`: Approve request, calculate `ExpiresAt`, generate secret token, return token once (`Approver`, `Admin`).
- `POST /api/requests/{id}/deny`: Deny request with optional reason (`Approver`, `Admin`).

### Audit Trail
- `GET /api/audit`: Paged, filterable audit log history ordered newest first (`Admin` only).

---

## Security Notes

1. **Password Security**: Passwords are never stored in plain text. They are hashed using ASP.NET Core's `PasswordHasher<User>` utilizing PBKDF2 with unique salts.
2. **One-Time Secret Grant Tokens**: Upon approval, the API generates a cryptographically secure 256-bit random token via `RandomNumberGenerator`. The token is returned **ONCE** in the HTTP response body and **only its SHA-256 hash** is saved in the database. Even database administrators cannot recover active grant secrets.
3. **Separation of Duties (SoD)**: An approver or admin is strictly blocked from approving or denying their own requests (`403 Forbidden`).
4. **Role-Based Access Control (RBAC)**: Enforced via ASP.NET Core JWT policies (`[Authorize(Roles = "...")]`).
5. **Immutable Audit Trail**: Every request creation, approval, denial, and automatic expiration writes an immutable row to `AuditLogs`. Automated background operations record `ActorUserId = null`.
6. **Thread Safety**: The background worker processes expirations in parallel using `IDbContextFactory<AppDbContext>`, ensuring concurrent worker threads do not share `DbContext` state.

---

## Running the Automated Tests

Run the full xUnit test suite using the .NET CLI:

```powershell
dotnet test
```

### Test Coverage Highlights
- **AccessRequestServiceTests**:
  - Validates `ExpiresAt` calculation (`DecidedAt + DurationMinutes`).
  - Verifies plain token is returned once and only its SHA-256 hash is persisted.
  - Verifies 409 Conflict when attempting to approve already-decided requests.
  - Verifies 403 Forbidden when an approver attempts to approve their own request.
  - Verifies status and timestamp transitions on denial.
  - Verifies immutable audit log creation for approvals and denials.
- **ExpiredGrantRevokerTests**:
  - Verifies expired approved grants transition to `Expired` and token hashes are cleared.
  - Verifies active non-expired grants remain `Approved`.
  - Verifies system audit log generation.
- **ValidationTests**:
  - Enforces the 1 to 480 minute duration rule boundaries (rejects <=0 and >480).
