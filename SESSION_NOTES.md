# Session Notes — 2026-07-16 (updated)

## Status at end of session

All work lives under `modernization/`. The original COG platform is untouched.

---

## Completed phases

| Phase | Component | Status |
|---|---|---|
| 0 | Foundation — shared domain, Terraform modules, CI skeleton | Done |
| 1 | auth-service (.NET 10, JWT, ASP.NET Core Identity) | Done |
| 2 | accounts-service + accounts-ui | Done |
| 3 | alerts-service (.NET 10 + SignalR, replaces InstantAction Node.js) | Done |
| 4 | admin-service + admin-ui | Done |
| 5 | lottery-service + lottery-ui + reports-service + reports-ui | Done |
| 6 | Database migrations + seed data | Done |
| 7 | Terraform staging environment + dev updates + Terraform CI | Done |
| 8 | EF Core C# migration files (all 6 services) | Done |
| 9 | Testcontainers integration tests (all 6 services) | Done |
| 10 | Playwright E2E specs (betting-ui + accounts-ui) | Done |
| 11 | Local dev hardening — all services running end-to-end | Done |

---

## What was built in the 2026-04-09 session

### Phase 5 — lottery-service, lottery-ui, reports-service, reports-ui
- lottery-service (port 5060): Permutations<T> ported, EF Core schema, balance check vs accounts-service, 17 tests
- lottery-ui (port 5176): Pick3/Pick4 purchase flow, history page, ticket detail
- reports-service (port 5070): Raw SQL wrapping legacy SPs, 6 tests
- reports-ui (port 5177): TanStack Table + Recharts, 3 report pages, 5 tests

### Phase 6 — Database Migrations
`database/migrations/` — idempotent T-SQL scripts (CREATE IF NOT EXISTS pattern):
- `auth-service/001_InitialCreate.sql`
- `accounts-service/001_InitialCreate.sql`
- `alerts-service/001_InitialCreate.sql`
- `admin-service/001_InitialCreate.sql`
- `lottery-service/001_InitialCreate.sql`

### Phase 7 — Terraform
All 6 modules implemented; dev + staging + prod environments; Terraform CI.

### Phase 8 — EF Core migration files (all services)
Each service has `InitialCreate.cs`, `InitialCreate.Designer.cs`, and `{Context}ModelSnapshot.cs`.

---

## What was built / fixed in the 2026-04-10 session

### Port reassignment — all services
All services were renumbered to avoid conflicts and to leave room between services.

| Service | Old port | New http port | New https port |
|---|---|---|---|
| auth-service | 5001 | 5010 | 5011 |
| accounts-service | 5002 | 5020 | 5021 |
| admin-service | 5003 | 5030 | 5031 |
| alerts-service | 5004 | 5040 | 5041 |
| betting-service | 5005 | 5050 | 5051 |
| lottery-service | 5006 | 5060 | 5061 |
| reports-service | 5007 | 5070 | 5071 |

### launchSettings.json — all services
Created `src/Properties/launchSettings.json` for every service with explicit http/https port bindings and `ASPNETCORE_ENVIRONMENT=Development`.

### Program.cs startup pattern — all services
Added `[STARTUP]` checkpoint log messages throughout `Program.cs` to make service startup visible and diagnose hangs. Pattern:
```
[STARTUP] Checking database connectivity...
[STARTUP] CanConnect = True
[STARTUP] Applying pending migrations...
[STARTUP] Migrations complete.
```
Also added `MaskPassword()` helper to log connection strings without exposing credentials.

### EF Core fixes — applied to all services
- `ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))` in DbContext setup
- Added `CanConnectAsync()` guard around `MigrateAsync()` to prevent startup hangs
- Wrapped migration block in try/catch so a DB connectivity failure doesn't crash startup

### Auth-service — SeedAdminUser migration
Manually created two files to bootstrap the admin user:
- `src/Migrations/20260410180000_SeedAdminUser.cs` — raw SQL INSERT (BCrypt hash for `Admin123!`, UserType=3 Employee, RoleId=5 Admin)
- `src/Migrations/20260410180000_SeedAdminUser.Designer.cs` — required `[DbContext]` + `[Migration]` attributes for EF Core migration discovery

Also manually registered `20260410165228_SyncModel` into `__EFMigrationsHistory` via raw SQL (database schema existed but history table was empty).

### SQL Server auth fix — all services
Changed all `appsettings.Development.json` connection strings from `Trusted_Connection=True` (causes SSPI errors on localhost) to explicit SQL auth:
```
Server=localhost,1433;Database=...;User ID=sa;Password=YourStrong!Passw0rd;Trust Server Certificate=True
```

### JWT authentication — all services
**Root problem:** .NET 10 JWT bearer defaults to `JsonWebTokenHandler` which rejects tokens without a `kid` header (IDX10517). Auth-service signs tokens using `JwtSecurityTokenHandler` which doesn't emit `kid`.

**Fix applied to all services (`AddJwtBearer`):**
```csharp
options.UseSecurityTokenValidators = true;  // force JwtSecurityTokenHandler
options.TokenValidationParameters = new TokenValidationParameters
{
    ...
    IssuerSigningKey = signingKey,
    IssuerSigningKeyResolver = (_, _, _, _) => [signingKey],  // belt-and-suspenders
    ClockSkew = TimeSpan.FromSeconds(30)
};
options.Events = new JwtBearerEvents
{
    OnAuthenticationFailed = ctx => { /* log warning */ }
};
```

### JWT SecretKey alignment
Both `appsettings.json` (base) and `appsettings.Development.json` for **all services** now use the same key:
```
6UZhMYYN1EqSmi7rtNvpRBS46WsNbVuYZwXzNft7h8c=
```
This ensures services work consistently regardless of whether `ASPNETCORE_ENVIRONMENT=Development` is set. Production key must be injected via `Jwt__SecretKey` environment variable from AWS Secrets Manager.

**Fingerprint diagnostic logging** added to `AddJwtAuthentication` in each service (logs SHA256 fingerprint of the validation key at startup).

### AutoMapper upgrade — accounts-service
- Upgraded from 13.0.1 (GHSA-rvv3-g6hj-g44x high severity vulnerability) to 16.1.1
- Fixed AutoMapper 14+ API: `AddAutoMapper(typeof(Program))` → `AddAutoMapper(cfg => cfg.AddMaps(typeof(Program).Assembly))`

### Package additions — accounts-service
- Added `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` v10.0.0 (required for `AddDbContextCheck`)

### README updates — all services
Updated all 7 service README files to reflect:
- Correct http/https ports
- JWT Secret Key section with dev key and production guidance
- Swagger token copy instructions (copy only `accessToken`, no `Bearer ` prefix)
- `dotnet run --launch-profile http` in Local Development section
- Ports table (http/https)
- `Jwt__Key` → `Jwt__SecretKey` throughout
- `Audience: "cog-platform"` → `"cog-services"` where incorrect
- `.NET 8 SDK` → `.NET 10 SDK` in betting-service
- Known Issues table (IDX10517 fix, SSPI fix)

---

## What was fixed in the 2026-04-11 session

### Phase 11 completion — admin, alerts, betting, lottery, reports services

All 5 remaining services fixed and verified (`dotnet build` clean, 0 errors):

**admin-service, alerts-service** (inline JWT in Program.cs):
- `Jwt:Key` → `Jwt:SecretKey` config key lookup
- Added `var signingKey` + `IssuerSigningKeyResolver = (_, _, _, _) => [signingKey]`
- Added `OnAuthenticationFailed` logging via `Log.Warning`
- `appsettings.json`: `Jwt:Key` → `Jwt:SecretKey`, aligned dev key, `"cog-platform"` → `"cog-services"`
- `appsettings.Development.json`: added full `ConnectionStrings` (SQL auth) + `Jwt` sections

**betting-service** (JWT in ServiceCollectionExtensions.cs):
- Already used `SecretKey` key name — fixed placeholder value to aligned dev key
- Added `var signingKey` + `IssuerSigningKeyResolver`
- Added `OnAuthenticationFailed` (uses `ILogger<JwtBearerEvents>` from DI)
- `appsettings.Development.json`: added full `ConnectionStrings` (SQL auth) + `Jwt` sections

**lottery-service**:
- `jwtSettings["Key"]` → `jwtSettings["SecretKey"]`
- Hardcoded accounts-service fallback URL port 5002 → 5020
- `appsettings.json`: key rename + value + audience + `Services:Accounts` port 5002 → 5020
- `appsettings.Development.json`: created (was missing) with SQL auth + Jwt + `Services:Accounts` pointing to localhost:5020

**reports-service**:
- `jwtSettings["Key"]` → `jwtSettings["SecretKey"]`
- `appsettings.json`: key rename + value + audience fix
- `appsettings.Development.json`: created (was missing) with SQL auth + Jwt

### admin-service — authorization policy fix (403 errors)
`CanViewConfig`, `CanEditConfig`, `CanManageUsers`, etc. were using invented lowercase policy names (`"config.view"`, `"users.view"`) that didn't match JWT claim values. Fixed to use `PermissionNames` PascalCase constants from auth-service:
```csharp
options.AddPolicy("CanViewUsers",     p => p.RequireClaim("permission", "Users.Manage", "Roles.Manage"));
options.AddPolicy("CanManageUsers",   p => p.RequireClaim("permission", "Users.Manage"));
options.AddPolicy("CanManageRoles",   p => p.RequireClaim("permission", "Roles.Manage"));
options.AddPolicy("CanViewConfig",    p => p.RequireClaim("permission", "System.Config"));
options.AddPolicy("CanEditConfig",    p => p.RequireClaim("permission", "System.Config"));
options.AddPolicy("CanViewAuditLogs", p => p.RequireClaim("permission", "Users.Manage", "System.Config"));
```

**reports-service** had same issue: `"reports.view"` → `"Reports.View", "Reports.Export"`.

### admin-service — entity/migration schema drift (500 errors)

Four divergences between entity classes and what the EF Core migrations had created:

1. **`Invalid object name 'Users'`** — `DbSet<ApplicationUser>` named `Users` → EF used `Users` as table name, but migration created `ApplicationUsers`. Fixed: added `b.ToTable("ApplicationUsers")` in `OnModelCreating`.

2. **`Invalid column name 'UpdatedByUserId'`** — `SystemConfiguration` entity had `int UpdatedByUserId` but migration created `nvarchar UpdatedBy`. Fixed: entity → `string? UpdatedBy`; `SystemConfigService` → `existing.UpdatedBy = updatedByUserId.ToString()`.

3. **`Invalid column name 'Username'` (AuditLog)** — entity had `Username` but no migration added it. Created `20260411000000_AddUsernameToAuditLog.cs` with `defaultValue: "system"`.

4. **`Invalid column name 'AssignedAt'`, `AssignedByUserId'`** — `UserRole` entity had these columns but no migration. Created `20260411000001_AddMissingColumns.cs` adding both with `defaultValueSql: "GETUTCDATE()"` and `defaultValue: 0`.

Both new migration `.cs` and `.Designer.cs` files created; `AdminDbContextModelSnapshot.cs` updated to include all new columns.

### accounts-service — T-SQL vs EF naming mismatch (500 errors)
Phase 6 T-SQL scripts created tables with singular/wrong names (e.g. `Customer` instead of `Customers`). EF Core migrations used different names. Resolution: dropped the `CogAccounts` database with `dotnet ef database drop --force` to let EF recreate it correctly from migrations.

---

## What was fixed in the 2026-04-14 session

### betting-service — ICurrentUser not registered (DI crash at startup)
`WagerService` took `ICurrentUser` as a constructor parameter but it was never registered. Created `HttpContextCurrentUser` reading `domain_id` → `AgentId`, `login_name` → `LoginName`, `ClaimTypes.Role` → `Roles` from the JWT claims via `IHttpContextAccessor`. Registered both in `AddApplicationServices`.

### betting-service / lottery-service — migration conflict (tables already exist)
T-SQL Phase 6 scripts had already created tables, but `__EFMigrationsHistory` was empty so EF tried to re-create them. Added idempotent SQL before `MigrateAsync()` to seed the history table with `20260409120000_InitialCreate` when the target table already exists.

### betting-service / lottery-service / reports-service — Swagger Bearer scheme
`SecuritySchemeType.ApiKey` sends the Authorization header value verbatim — users had to type `Bearer <token>`. Changed to `SecuritySchemeType.Http` + `Scheme = "bearer"` so Swagger UI prepends the prefix automatically (same as admin/accounts services).

### All 7 services — health endpoints
- auth-service and betting-service were using `UseHealthChecks` (old middleware); converted to `MapHealthChecks` (endpoint routing)
- auth-service was still using the `CanConnectAsync` guard; replaced with direct `MigrateAsync()`

### All 7 K8s manifests — consistency pass
Fixed lottery-service and reports-service manifests (old port 5006/5007, `Jwt__Key`, `cog-platform` audience, bare `cog/...:latest` image). Fixed admin-service and alerts-service manifests (same issues). All 7 now use:
- `image: ${ECR_REGISTRY}/cog/<svc>:${IMAGE_TAG}`
- `containerPort: 8080`
- `Jwt__SecretKey` from `cog-jwt-secret`
- `Audience: cog-services`

### database/seed/05_system_config_defaults.sql — placeholder removed
`smtp.password` value changed from `[REPLACE_BEFORE_GOLIVE]` to empty string with description pointing to `Email__Password` env var / `smtp-secret` K8s secret.

### CanConnectAsync guard removed — accounts-service, admin-service, alerts-service
**Root problem:** startup guard called `CanConnectAsync()` before `MigrateAsync()`. When the database doesn't exist yet (freshly dropped or first run), `CanConnectAsync()` returns `false` → `MigrateAsync()` is skipped → service starts without a DB → every request fails with `RetryLimitExceededException: Login failed`.

**Fix:** removed the `CanConnectAsync()` gate; now calls `MigrateAsync()` directly. `MigrateAsync()` creates the DB if absent and applies all pending migrations. The surrounding `try/catch` handles genuine SQL Server unreachability.

Applied to: `accounts-service/src/Program.cs`, `admin-service/src/Program.cs`, `alerts-service/src/Program.cs`.

---

## Critical: local dev startup order

Services must be started with `--launch-profile http` to load `appsettings.Development.json`:
```bash
dotnet run --launch-profile http
```

**`IOptions<T>` does NOT hot-reload.** If you change `appsettings.json`, you MUST restart the service. Auth-service must be restarted before getting new tokens — old tokens signed with a stale key will fail validation in other services.

---

## Known local issues

| Issue | Status |
|---|---|
| Vitest workers don't start on Windows/Node 24 for React UIs | Existing — pass in CI (Ubuntu/Node 20) |
| JWT key mismatch if auth-service not restarted after key change | Fixed — both `appsettings.json` and `appsettings.Development.json` now use same key; restart auth-service + get fresh token |

---

## What was built in the 2026-04-14 session (continued)

### casino-service (new — port 5080 / 5081)
Full Live Dealer backend replacing `crazyhorse/index.asp`:
- Two-step deposit/withdraw wrapping the external XML API (`ittds.newland.cr`)
- `LiveDealerXmlClient` — `XDocument` parsing, `WebUtility.UrlEncode`, idempotency via `transferProcessedEarlier`
- `CasinoService` — init → confirm flow with rollback via `IAccountsClient.RollbackDocumentAsync`
- EF Core: `CasinoPlayers`, `CasinoTransactions` tables; unique index on (CustomerId, CasinoId)
- K8s manifest + Dockerfile; 10 unit tests pass

### casino-ui (new — port 5178)
Live Dealer SPA — dark casino theme (gray-900 / yellow-400):
- `LoginPage` — username/password → POST `/api/auth/login` → Zustand auth store
- `SetupPage` — first-time nickname registration → POST `/api/Casino/register`
- `CasinoLobbyPage` — balance cards, deposit/withdraw modals, "Play Now" link opening lobbyUrl
- `RequireAuth` route guard; TanStack Query with 60s balance auto-refresh
- 9 unit tests (LoginPage, SetupPage, CasinoLobbyPage)

### shared/ui-components (new)
Shared React component library (`@cog/ui-components`):
- `Button` (4 variants, 3 sizes, loading spinner)
- `Input` (error state, prefix character)
- `FormField` (label + hint + error wrapper)
- `Badge` (5 variants)
- `Modal` (Escape / backdrop close, accessible role=dialog)
- `LoadingSpinner` (3 sizes, sr-only label)
- `Alert` (4 variants, dismissible)
- `PageHeader` (title + subtitle + actions slot)
- `NavBar` (brand + nav links + actions slot)
- `DataTable` (generic typed columns, custom render, loading/empty states)
- `cn()` utility (clsx + tailwind-merge)
- 32 unit tests (Button×6, Input×5, Badge×4, Modal×6, Alert×6, DataTable×5, cn×4)

---

## What was fixed in the 2026-04-14 session (second pass)

### All 6 UI Vite proxy configs — stale ports corrected
All UIs were pointing to old (pre-renumber) service ports. Updated `vite.config.ts` for every UI:

| UI | Old proxy | New proxy |
|---|---|---|
| admin-ui | `/api` → `:5003` | `/api/auth` → `:5010`; `/api` → `:5030` |
| accounts-ui | `/api` → `:5002` | `/api/auth` → `:5010`; `/api` → `:5020` |
| betting-ui | port `3000`; `/api`+`/hubs` → `:5001` | port `5173`; `/api/auth` → `:5010`; `/api` → `:5050`; `/hubs` → `:5040` (ws) |
| lottery-ui | `/api` → `:5006` | `/api` → `:5060` |
| reports-ui | `/api` → `:5007` | `/api` → `:5070` |
| casino-ui | `/api` → `:5080` | already correct |

Auth-service calls (`/api/auth/*`) now use a separate higher-priority proxy rule, because each UI's own backend service doesn't host the `/auth` routes.

### All 6 UIs — ESLint v9, Vite v6, dependency upgrades
All UIs were on ESLint v8 (deprecated), causing multiple `npm warn deprecated` messages on install and 8 security vulnerabilities from Vite v5.

Changes per UI (`package.json` + new `eslint.config.js`):
- `eslint` v8 → v9 (flat config format)
- Removed `@typescript-eslint/parser` + `@typescript-eslint/eslint-plugin`; replaced with `typescript-eslint ^8` (combined package)
- Added `@eslint/js ^9` and `globals ^15`
- `eslint-plugin-react-hooks` v4 → v5
- `eslint-plugin-react-refresh` → `^0.4.18`
- `vite` v5 → v6 (addresses CVEs)
- `@vitejs/plugin-react` → `^4.3.4`
- `axios` → `^1.8.0`, `react`/`react-dom` → `^18.3.1`, `typescript` → `^5.7.3`
- Lint script: removed `--ext ts,tsx --report-unused-disable-directives` (handled by flat config)

**Remaining deprecation warning:** `whatwg-encoding` — transitive dep of `jsdom`; cannot be fixed without jsdom releasing an update. No security impact.

### admin-ui — login 400 Bad Request (field name mismatch)
Auth-service expected `loginName` in the request body; admin-ui was sending `username` → 400 validation failure.

Auth-service's `AuthTokenResponse` is flat (`userId`, `loginName`, `userType`, `domainEntityId`, `roles`, `permissions`); admin-ui `LoginPage` was expecting a nested `{ user: { id, username, email } }` shape.

Files changed:
- `src/pages/LoginPage.tsx` — request body: `{ loginName: data.username, password }`; response mapped to store shape
- `src/store/authStore.ts` — `AuthUser` interface: removed `username`/`email`; added `loginName`, `userType`, `domainEntityId`
- `src/layouts/DashboardLayout.tsx` — `user.username` → `user.loginName`
- `src/test/authStore.test.ts` — updated `setUser` calls and assertions to match new interface

Note: `UserDto` in `src/api/users.ts` was NOT changed — admin-service's user management API correctly uses `username`/`email` for its own user list.

---

---

## What was built / fixed in the 2026-04-17 session

### Agent creation 400 fix — `JsonStringEnumConverter`
`System.Text.Json` serializes enums as integers by default. The frontend sends string enum values (`"Agent"`, `"WeeklyProfit"`, etc.) which caused 400 errors on all enum-bearing request bodies.

Fix: added `JsonStringEnumConverter` globally in `accounts-service/src/Program.cs`:
```csharp
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
```
Affects: `AgentType`, `CommissionType`, `TransactionCode`, `TransactionType` — all enums in request bodies now deserialize correctly from strings.

### All UIs — dark sidebar style applied (admin-ui pattern)
Applied the admin-ui dark sidebar pattern (`bg-gray-900`, `bg-indigo-600` active, `text-gray-300` inactive) to all other UIs:
- `accounts-ui/src/components/Layout.tsx` — full rewrite to dark sidebar
- `reports-ui/src/layouts/DashboardLayout.tsx` — slate → gray-900/indigo
- `betting-ui/src/components/layout/MainLayout.tsx` — dark sidebar, yellow wager-slip badge preserved
- `casino-ui/src/components/layout/MainLayout.tsx` — `bg-gray-950`, yellow-600 active to preserve casino branding

### accounts-service + accounts-ui — agent position & figures (task #22/#23)

**Backend (accounts-service):**
- `AgentPositionResponse.cs` — per-agent position snapshot (customer count, total balance, pending wagers, free play, credit)
- `AgentPositionSummaryResponse.cs` — compact list for portfolio overview
- `AgentFiguresResponse.cs` — period-over-period figures with per-customer breakdown
- `AgentService.cs` — added `GetPositionAsync`, `GetPositionSummaryAsync`, `GetFiguresAsync`
  - Key fix: `db.CustomerTransactions` (not `db.Transactions`); `.ThenInclude` for nested nav props
- `AgentsController.cs` — added `GET /agents/position-summary`, `GET /agents/{id}/position`, `GET /agents/{id}/figures`

**Frontend (accounts-ui):**
- `types/accounts.ts` — added `AgentPosition`, `AgentPositionSummary`, `CustomerFiguresLine`, `AgentFigures`
- `api/accountsApi.ts` — added `getAgentPositionSummary`, `getAgentPosition`, `getAgentFigures`
- `pages/agent-tabs/FiguresTab.tsx` — position cards + date-range figures with per-customer table
- `pages/PositionPage.tsx` — portfolio overview: all-agent summary cards + table with utilisation % colour coding
- `pages/AgentDetailPage.tsx` — added Figures tab
- `App.tsx` — added `/position` route

### accounts-service + accounts-ui — settlement page (task #21)
- `pages/SettlementPage.tsx` — week-ending date picker, Calculate All button, per-agent rows with inline result state
  - AgentRow: calculate → confirm sequential flow; status badges (Pending/Calculated/Confirmed)
  - `handleCalculateAll`: sequential async loop avoiding rate-limit hammering
- `App.tsx` — added `/settlement` route

### Testcontainers integration test timeout fix
`AccountsApiFactory.InitializeAsync()` was timing out after 60s (default). SQL Server container needs 2–3 min to initialize.
Fix: `.WithStartupTimeout(TimeSpan.FromMinutes(5))` on `MsSqlBuilder`.
Result: 28/36 pass (8 failures unrelated — separate DB constraint issues).

### betting-ui npm install fix
`@vitest/ui@^4.1.3` conflicted with `vitest@^3.2.4` (must match major version).
Fix: changed `"@vitest/ui": "^4.1.3"` → `"^3.2.4"` in `betting-ui/package.json`.
Used `npx rimraf` to clear locked `node_modules` (Windows `ENOTEMPTY`/`EPERM` blocked `rm -rf`).
Install completed: 0 vulnerabilities, exit 0.

### Task #25 — Extended customer management tabs
`CustomerDashboardPage.tsx` wired up three additional tabs:
- `PermissionsTab` — feature flags (instant action, live dealer, parlays, etc.)
- `FreePlayTab` — free play balance history and award form
- `CommentsTab` — internal agent notes with add/view
Tab bar gains `overflow-x-auto` + `min-w-max` for horizontal scroll on small screens.

### Task #29 — Auth provisioning for admin-created users
Admin-service creates users into `COGDB_Admin.dbo.ApplicationUsers`; auth-service reads `COGDB_Auth.dbo.Users`. Login failed for admin-created users because no auth record existed.

Fix:
- `auth-service`: new `POST /api/users` endpoint (`UsersController`) + `ProvisionUserRequest` DTO
- `admin-service`: new `AuthProvisioningService` (HTTP POST to auth-service), `IAuthProvisioningService` interface
- `admin-service/UserService.CreateUserAsync`: calls provisioning after saving `ApplicationUser`; non-fatal on failure (logs warning, continues)
- Config: `Services:AuthService:BaseUrl` in `appsettings.json`

### Task #26 — Batch transaction entry (complete)

**Backend (accounts-service):**
- `BatchCreateTransactionRequest.cs` — list of `CreateTransactionRequest` + `StopOnFirstError` flag
- `BatchTransactionResponse.cs` + `BatchTransactionLineResult.cs` — per-row success/failure with error details
- `ITransactionService.CreateBatchAsync` → `TransactionService.CreateBatchAsync` — sequential execution, collects per-row results
- `POST /api/transactions/batch` — max 200 rows, validates non-empty; returns 200 with full results (partial success allowed)

**Frontend (accounts-ui):**
- `types/accounts.ts` — added `BatchCreateTransactionRequest`, `BatchTransactionLineResult`, `BatchTransactionResponse`
- `api/accountsApi.ts` — added `createBatchTransactions`
- `pages/BatchTransactionsPage.tsx` — spreadsheet-style table
  - Dynamic rows (add/remove, +5 bulk add)
  - Per-row: customer ID, Code (Credit/Debit), Type selector, Amount, Description, Reference, Payment Method
  - Running credit/debit totals in header
  - Per-row ✓/✗ status after submit; failed row error details listed below table
  - Stop-on-first-error toggle
  - Clear button resets all state
- `App.tsx` — route `/transactions/batch`
- `Layout.tsx` — "Batch Transactions" nav link

---

## Remaining work

### Medium term
- Set real secrets in AWS Secrets Manager before first deploy: JWT key, SMTP password, DB password
  - SMTP password is stored empty in `05_system_config_defaults.sql`; inject via `Email__Password` env var from `smtp-secret` K8s secret

### Longer term
- Build `Cog.Observability` NuGet package (Serilog + CloudWatch + X-Ray)
- Migration validation: run legacy + new systems in parallel for 2 weeks
- Wager payout validation: compare calculations for 100% match against legacy

---

## File layout

```
modernization/
├── .github/workflows/
│   ├── dotnet-services.yml   — CI for 7 .NET services
│   ├── frontend-apps.yml     — CI for 5 React UIs
│   └── terraform.yml         — Terraform validate + plan + apply
├── shared/domain/            — Shared C# entity library
├── auth-service/             — JWT auth (http:5010 / https:5011)
├── betting-service/          — Wagers, games, lines (http:5050 / https:5051)
├── betting-ui/               — Web betting SPA (port 5173)
├── accounts-service/         — Customers, transactions, agent hierarchy (http:5020 / https:5021)
├── accounts-ui/              — Account management SPA (port 5174)
├── alerts-service/           — SignalR real-time alerts (http:5040 / https:5041)
├── admin-service/            — Users, roles, config, audit (http:5030 / https:5031)
├── admin-ui/                 — Admin SPA (port 5175)
├── lottery-service/          — Pick3/Pick4 lottery (http:5060 / https:5061)
├── lottery-ui/               — Lottery SPA (port 5176)
├── reports-service/          — Wager/transaction/agent reports (http:5070 / https:5071)
├── reports-ui/               — Reports dashboard SPA (port 5177)
├── casino-service/           — Live Dealer integration (http:5080 / https:5081)
├── casino-ui/                — Live Dealer SPA (port 5178)
├── shared/
│   ├── domain/               — Shared C# domain entities (Cog.Domain)
│   └── ui-components/        — Shared React component library (@cog/ui-components)
├── database/
│   ├── migrations/           — Per-service DDL scripts
│   ├── seed/                 — Reference data (5 files)
│   └── README.md
└── terraform/
    ├── modules/              — 6 fully-implemented modules
    ├── environments/
    │   ├── dev/
    │   ├── staging/
    │   └── prod/
    └── README.md
```

---

---

## What was built / fixed in the 2026-04-18 session

### Task #16 — Game management CRUD in betting-service (complete)

**New request models (`betting-service/src/Models/Requests/GameRequests.cs`):**
- `CreateGameRequest` — sport, teams, date, rotation, optional periods list
- `UpdateGameRequest` — teams, date, rotation (guards against Final/Cancelled status)
- `UpdateGameStatusRequest` — status transition
- `CreateGamePeriodRequest` — description + period number
- `CreateSportTypeRequest` / `UpdateSportTypeRequest` — name, code (uppercase-normalised), active flag, display order

**`IGameService.cs` / `GameService.cs` — 8 new methods:**
- `CreateGameAsync` — validates sport exists, creates game + any initial periods
- `UpdateGameAsync` — blocks edits on Final/Cancelled games
- `UpdateGameStatusAsync` — unrestricted status transition
- `DeleteGameAsync` — blocks in-progress games and games with wagers (checks across all periods)
- `AddPeriodAsync` — blocks on Final/Cancelled game
- `RemovePeriodAsync` — blocks if period has wagers
- `CreateSportTypeAsync` — enforces unique code
- `UpdateSportTypeAsync` — enforces unique code, allows deactivation

**`GamesController.cs` — 10 endpoints:**

| Method | Route | Role |
|---|---|---|
| GET | `/api/games` | Any auth |
| GET | `/api/games/{id}` | Any auth |
| GET | `/api/games/sports?includeInactive` | Any auth |
| POST | `/api/games` | Admin, LinesManager |
| PUT | `/api/games/{id}` | Admin, LinesManager |
| PATCH | `/api/games/{id}/status` | Admin, LinesManager |
| DELETE | `/api/games/{id}` | Admin only |
| POST | `/api/games/{id}/periods` | Admin, LinesManager |
| DELETE | `/api/games/{id}/periods/{periodId}` | Admin, LinesManager |
| POST | `/api/games/sports` | Admin only |
| PUT | `/api/games/sports/{sportId}` | Admin only |

**`BettingDbContext.cs`** — added `CustomerBalances` and `CustomerLimits` DbSets (required by integration tests).

**Pre-existing bug fixed — `WagerService.GetLineForItem`:**
Spread bets were using `lineSet.Spread` (the point spread value, e.g. -3.5) instead of `lineSet.SpreadJuice` (-110) for payout calculation. This caused ~3143x overbilling. Also fixed Total bets to use `OverJuice`/`UnderJuice` instead of the Total line value.

**Test infrastructure fixed (`BettingService.Tests.csproj`):**
The test project was on `net8.0` and had never successfully compiled against the `net10.0` service. Updated:
- `TargetFramework`: `net8.0` → `net10.0`
- `Microsoft.EntityFrameworkCore.InMemory`: `8.0.0` → `10.0.0`
- `Microsoft.AspNetCore.Mvc.Testing`: `8.0.0` → `10.0.0`
- `Testcontainers.*`: `3.7.0` → `4.1.0`
- xunit: `2.6.4` → `2.9.3`
- Added `<Using Include="Xunit" />` global using (AutoMapper 16 also required `NullLoggerFactory` as 2nd arg to `MapperConfiguration`)

**`GameServiceTests.cs`** — 16 unit tests, all pass. WagerService tests: 5/5 pass.

---

## Service port map (updated 2026-04-10)

| Service | http | https |
|---|---|---|
| auth-service | 5010 | 5011 |
| accounts-service | 5020 | 5021 |
| admin-service | 5030 | 5031 |
| alerts-service | 5040 | 5041 |
| betting-service | 5050 | 5051 |
| lottery-service | 5060 | 5061 |
| reports-service | 5070 | 5071 |
| betting-ui | 5173 | — |
| accounts-ui | 5174 | — |
| admin-ui | 5175 | — |
| lottery-ui | 5176 | — |
| reports-ui | 5177 | — |
| casino-service | 5080 | 5081 |
| casino-ui | 5178 | — |

---

## What was built / fixed in the 2026-05-09 session

### Bug fix — GameFormModal sport dropdown empty (betting-ui)
`SportTypeResponse` DTO in `betting-service/src/Models/Responses/GameResponse.cs` was missing `IsActive` and `DisplayOrder` fields.
The frontend `GameFormModal` filtered `sports.filter(s => s.isActive)` — with `isActive` always `undefined`, the dropdown was always empty.
Fix: added `IsActive` and `DisplayOrder` to `SportTypeResponse`.

### Bug fix — Game card shows only date, no teams (betting-ui)
`GameSelectionPage.tsx` rendered `game.periods.map(p => { if (!lines) return null; ... })` — when no period had lines set, every period returned `null` and the card rendered nothing but the date.
Fix: added an outer check `game.periods.every(p => !p.lines)` at the card level; renders team names + "Lines not yet available" label when no period has lines yet.

### Feature — accounts-service → betting-service provisioning sync

**Problem:** accounts-service saves agents/customers to `CogAccounts` DB; betting-service needs them in `COGDB_Betting` before wagers can be placed.

**betting-service — new internal endpoints (`src/Controllers/InternalController.cs`):**
- `POST /api/internal/agents` — idempotent agent provisioning; returns 200 if already exists
- `POST /api/internal/customers` — creates Customer + CustomerBalance + CustomerLimits; looks up agent by LoginName
- Both require `[Authorize(Roles = "Admin")]`
- Request DTOs: `ProvisionAgentRequest.cs`, `ProvisionCustomerRequest.cs`

**accounts-service — `BettingProvisioningService.cs` (same pattern as `AuthProvisioningService`):**
- `IBettingProvisioningService` interface with `ProvisionAgentAsync` and `ProvisionCustomerAsync`
- Authenticates with auth-service (using `Services:AuthService:ServiceAccount` credentials) to get JWT
- Uses JWT to POST to betting-service internal endpoints
- Non-fatal: any exception logs warning and returns without blocking local account creation
- Registered as singleton in `ServiceCollectionExtensions.AddServiceClients()`
- Named HttpClients: `"auth-service"` (port 5010), `"betting-service"` (port 5050)

**accounts-service wiring:**
- `AgentService.CreateAgentAsync` — calls `ProvisionAgentAsync` after `SaveChangesAsync`
- `CustomerService.CreateCustomerAsync` — calls `ProvisionCustomerAsync` after `SaveChangesAsync`

**Config added to `appsettings.json`:**
```json
"Services": {
  "AuthService": {
    "BaseUrl": "http://localhost:5010",
    "ServiceAccount": { "Username": "admin", "Password": "REPLACE_WITH_ADMIN_PASSWORD" }
  },
  "BettingService": { "BaseUrl": "http://localhost:5050" }
}
```
Dev password (`Admin123!`) is in `appsettings.Development.json`.

Both services build with 0 errors, 0 warnings.

### Task #18 — Wager grading endpoint (betting-service) — complete

**New endpoints:**
- `POST /api/games/{id}/grade` — grades a game by accepting per-period scores; requires `Admin` or `LinesManager` role
  - Marks game status `Final`
  - Grades each pending `WagerItem` (Won/Lost/Push/NoAction) based on scores + line type
  - Grades parent `Wager` (Won/Lost/Push) — multi-game parlays stay `Pending` until all legs resolved
  - Returns `GradeGameResponse` with counts and total payout
- `GET /api/wagers/graded?agentId=&gameId=&page=&pageSize=` — returns all Won/Lost/Push wagers for LinesManager graded view

**New files:**
- `src/Models/Requests/GradeGameRequest.cs` — `{ periodScores: [{ periodId, homeScore, awayScore }] }`
- `src/Models/Responses/GradeGameResponse.cs` — `{ gameId, wagersGraded, wagersWon, wagersLost, wagersPushed, totalPayout }`
- `src/Services/WagerGradingEngine.cs` — pure static grading logic (`internal` for unit-testability)
- `src/AssemblyInfo.cs` — `[assembly: InternalsVisibleTo("BettingService.Tests")]`

**Grading logic (`WagerGradingEngine`):**
- **Spread**: `coverMargin = homeScore - awayScore + spread`; `> 0` → home covered, `< 0` → away covered, `= 0` → push
- **MoneyLine**: winner by score diff; tie → push
- **Total**: combined score vs total line; over/under/push
- **Straight payout**: Won = risk + win; Push = risk; Lost = 0
- **Parlay payout**: compound product of American-to-decimal odds for winning legs; push legs excluded; any loss = 0; all push = return stake

**`WagerResponse`** — added `GradedBy` field.

**Tests:** 20 new unit tests in `tests/WagerGradingEngineTests.cs` — all pass.
All enum values corrected from exploration-agent-reported values to actual domain values (`WagerStatus.Won/Lost/Push/NoAction`, `WagerItemStatus.Won/Lost/Push/NoAction`).

---

## What was built / fixed in the 2026-07-15 session

### Test project modernization — admin-service, accounts-service, alerts-service, lottery-service, reports-service

Applied the same `net10.0` test-project fix previously used for betting-service (2026-04-18 session) to the remaining five services. Verified with a clean `dotnet build` + `dotnet test` pass on all five:

| Service | Build | Unit tests |
|---|---|---|
| admin-service | 0 errors, 0 warnings | 17/17 passed |
| accounts-service | 0 errors, 0 warnings | 28/28 passed |
| alerts-service | 0 errors, 0 warnings | 19/19 passed |
| lottery-service | 0 errors, 0 warnings | 16/16 passed |
| reports-service | 0 errors, 0 warnings | 7/7 passed |

Total: 87 unit tests passing across the five services (integration tests excluded from this run — they require Testcontainers/Docker).

**New/updated test files:**
- `admin-service/tests/Unit/UserServiceTests.cs` — 10 tests
- `alerts-service/tests/Unit/AlertDataServiceTests.cs` — 11 tests
- `accounts-service/tests/Unit/CustomerServiceTests.cs` — 12 tests
- `accounts-service/tests/Unit/AgentServiceTests.cs` — 8 tests
- `accounts-service/tests/Integration/AccountsIntegrationTests.cs` — 8 tests (Testcontainers)
- `lottery-service/tests/Unit/LotteryServiceTests.cs` — 9 tests
- `lottery-service/tests/Integration/LotteryIntegrationTests.cs` — 8 tests (Testcontainers)

**csproj changes** (`admin-service`, `accounts-service`, `alerts-service`, `lottery-service`, `reports-service` — all under `tests/`):
- `TargetFramework`: aligned to `net10.0`
- `Microsoft.EntityFrameworkCore.InMemory` → `10.0.0`
- `Microsoft.AspNetCore.Mvc.Testing` → `10.0.0`
- `Testcontainers.MsSql` → `4.1.0`
- accounts-service additionally moved to the newer xunit/FluentAssertions/NSubstitute stack (xunit `2.9.3`, `xunit.runner.visualstudio` `3.0.1`, `FluentAssertions` `7.2.0`, `NSubstitute` `5.3.0`, `Microsoft.NET.Test.Sdk` `17.12.0`) — admin-service/alerts-service/lottery-service/reports-service remain on the Moq-based stack (xunit `2.6.2`, `Moq` `4.20.69`, `Microsoft.NET.Test.Sdk` `17.8.0`)

### betting-ui — `package-lock.json` refreshed
Lockfile regenerated via `npm install`; no dependency version changes (still `@vitest/ui ^3.2.4` matching `vitest ^3.2.4`).

---

## What was fixed in the 2026-07-16 session — running integration tests against Docker

Ran the Testcontainers-backed integration suites (auth, admin, accounts, alerts, betting, lottery — the six services with an `Integration` test folder) against a real Docker daemon for the first time. All 48 tests failed. Root-caused and fixed multiple distinct bugs, in order of discovery:

### Bug 1 — every fixture pointed EF Core at `master`, not a real test database
`MsSqlContainer.GetConnectionString()` returns a connection string with no `Initial Catalog`, so EF Core treated `master` as the application database. `ResetDatabaseAsync()`'s `EnsureDeletedAsync()` then tried `ALTER DATABASE master SET SINGLE_USER` to drop it, which SQL Server refuses outright (`Option 'SINGLE_USER' cannot be set in database 'master'`) — every test failed before any app code ran.

**Fix** — added a `GetTestConnectionString()` helper to each of the 6 fixtures that rebuilds the connection string with an explicit `InitialCatalog` (`AccountsServiceTest`, `AdminServiceTest`, `AlertsServiceTest`, `AuthServiceTest`, `BettingServiceTest`, `LotteryServiceTest`) via `SqlConnectionStringBuilder`, and used it in place of the raw `_sql.GetConnectionString()` call in `ConfigureWebHost`. Files: `{accounts,admin,alerts,auth,betting,lottery}-service/tests/Integration/*IntegrationTests.cs`.

Re-running after this fix moved several services from 0/N passing to partial passes, confirming the bug and surfacing the real bugs underneath it (below). auth-service's first re-run still showed 0/9, but that turned out to be Docker/Testcontainers API contention from running multiple `dotnet test` invocations against Docker at the same time — resolved by not running Testcontainers suites concurrently.

### Bug 2 — lottery-service: EF migration had drifted completely from the entity model
`DrawingDetails`, `LotteryTickets` etc. as defined in `20260409120000_InitialCreate.cs` used an entirely different shape than the current entities (e.g. migration had `DrawingTime`/`CutoffTime`/`IsOpen`/`IsDrawn`/`WinningNumbers` and `int` keys; entities now have `DrawingDate`/`MinutesToDraw`/`IsActive` and `long` keys). Every insert failed with `Invalid column name`.

**Fix** — deleted the stale migration + model snapshot and regenerated from the current model:
```
dotnet ef migrations add InitialCreate --project src --startup-project src -o Migrations
```
New migration: `lottery-service/src/Migrations/20260716181336_InitialCreate.cs`. (Global `dotnet-ef` tool is v8.0.26 vs the project's EF Core 10.0.0 — works with a version-mismatch warning; consider `dotnet tool update -g dotnet-ef` in a future session.)

### Bug 3 — betting-service: seed data violated IDENTITY_INSERT
`BettingApiFactory.ResetDatabaseAsync()` inserts a base `Agent` with an explicit `Id = 1` (required for customer FK references throughout the tests), but `Agents.Id` is an IDENTITY column — every test failed at seed time with `Cannot insert explicit value for identity column`.

**Fix** — wrapped the seed insert in a transaction with `SET IDENTITY_INSERT [Agents] ON` / `OFF` around `SaveChangesAsync()`. File: `betting-service/tests/Integration/WagersIntegrationTests.cs`.

### Bug 4 — no service actually implemented the test auth bypass
Every fixture sent `Authorization: Bearer test-bypass-token`, but grepping all `Program.cs` files confirmed **no service has any `Testing`-environment auth handling** — `test-bypass-token` is not a valid JWT, so real JWT-bearer validation rejected it and most authenticated endpoints returned 401. betting-service's fixture already had a `GenerateTestJwt()` stub with a comment describing the intended design ("services should accept a well-known test token") but it just returned the same literal string.

**Fix** — since all 5 non-auth services share the same dev JWT secret/issuer/audience (`appsettings.json` → `Jwt:SecretKey = 6UZhMYYN1EqSmi7rtNvpRBS46WsNbVuYZwXzNft7h8c=`, `Issuer = cog-auth-service`, `Audience = cog-services`), replaced the fake token with a real `JwtSecurityToken` minted in each fixture, matching the claim shape `AuthService.Services.TokenService` actually issues (`sub`, `jti`, `login_name`, `user_type`, `domain_id`, `ClaimTypes.Role` per role, `permission` per permission):
- **accounts-service** — roles `Admin`, `MasterAgent`, `Agent`
- **admin-service** — role `Admin` + permissions `Users.Manage`, `Roles.Manage`, `System.Config` (matches its `RequireClaim("permission", ...)` policies)
- **alerts-service** — role `Admin`; also added `AccessTokenProvider` to the SignalR `HubConnectionBuilder` in `SignalR_CanConnect` (the hub is `[Authorize]` and the server already supports token-via-query-string for `/hubs/*`, it just wasn't being sent)
- **betting-service** — roles `Admin`, `LinesManager`, `domain_id`/`login_name` = the seeded base agent (matches `ICurrentUser`)
- **lottery-service** — added `customerId`/`agentId` claims (not `domain_id`) since `TicketsController` reads those specific claim names directly via `User.FindFirstValue`

Files: `GenerateTestJwt()` added to each `*ApiFactory` class in the same 5 `*IntegrationTests.cs` files as Bug 1.

### Bug 5 — alerts-service: tests expected endpoints that don't exist
- `POST /api/alerts` (plain create) — controller only had `POST /api/alerts/broadcast` (SignalR broadcast) and `POST /api/alerts/unalert/{customerId}`. No endpoint persisted a new alert.
- `DELETE /api/alerts/{id}` (dismiss) — no delete endpoint existed at all.
- `GET /api/alerts/vip-settings/{agentId}` — test hit a URL that never existed; the real route is `GET /api/alerts/vip/{agentId}`.

**Fix**:
- Added `CreateAlertRequest` DTO (`Models/AlertTicketDto.cs`), `CreateAlertAsync`/`DismissAlertAsync` on `IAlertDataService`/`AlertDataService` (insert/remove an `AlertTicket` row), and `[HttpPost]`/`[HttpDelete("{id:int}")]` actions on `AlertsController`.
- Fixed the test's VIP-settings URL to `/api/alerts/vip/1` (test-side fix, matching the real route).

### Bug 6 — admin-service: test used PATCH, controller only supports PUT
`UpdateUser_ValidRequest_Returns200` called `PatchAsJsonAsync`; `UsersController.UpdateUser` is `[HttpPut("{id:int}")]`. Fixed the test to use `PutAsJsonAsync` (test-side fix — PUT is the correct, already-implemented verb).

### Bug 7 — lottery-service: test assumed a customer-scoped ticket-list route that doesn't exist
`GetCustomerTickets_Returns200` queried `GET /api/lottery/tickets/{customerId}`, but `TicketsController` only has `GET /api/lottery/tickets/{id:long}` (single ticket by ticket ID) and `GET /api/lottery/tickets/my` (claims-based, using the `customerId` JWT claim). The test would have accidentally hit the single-ticket route and failed. Rewrote the test to purchase a ticket and then call `/api/lottery/tickets/my`, matching the actual claims-based design (and no longer sends the now-irrelevant `CustomerId` in the purchase body).

### Docker environment instability
Mid-session, `auth-service`'s integration tests began hanging indefinitely (its SQL Server container's `sqlcmd -Q "SELECT 1"` health check retried for 8+ minutes without ever succeeding), and this later spread to other services failing near-instantly with `HttpRequestException`/`TimeoutException` on container start. Root cause: Docker Desktop/WSL2 degraded after very heavy same-session Testcontainers churn (dozens of SQL Server container spin-ups across many re-runs, plus two hung `dotnet test` processes that had to be force-killed). **A full Docker Desktop restart resolved it.** Lesson: never run two Testcontainers-based `dotnet test` invocations concurrently — even accidentally overlapping a quick diagnostic run with a still-running background suite caused contention and corrupted results (e.g. a "0/14 passed" betting-service result that was actually just contention, not a real regression).

### GitHub repository created
Pushed `modernization/` (not the legacy trunk) to a new public repo: **https://github.com/esolmo/cog-platform-modernization**. Scope and visibility (public, secrets included as-is) were both explicit user decisions — the repo has hardcoded dev secrets (shared JWT key, SQL `sa` passwords, default admin password) committed in plain text in `appsettings.Development.json` files and this notes file; rotate before any real deployment.

## Follow-up fixes — same 2026-07-16 session, after Docker restart

With a clean Docker daemon, re-ran and fixed the remaining real bugs (not infra) service by service:

### alerts-service → 8/8 (was 7/8)
`AlertTicketDto` was missing an `AgentId` property entirely — `GetAlerts_ReturnsOnlyAgentAlerts` always saw `AgentId: 0` client-side. Added `AgentId` to the DTO and to `AlertDataService.MapToDto`; fixed a unit test (`EmailServiceTests.MakeTicket()`) that constructed the DTO positionally.

### admin-service → 10/10 (was 6/10)
- `CreateUser_ValidRequest_Returns201` / `CreateUser_DuplicateUsername_Returns409`: test payloads omitted `MaxAccessLevel` and `RoleIds`, both required (non-nullable) fields on `CreateUserRequest` → `[ApiController]` auto-400'd before reaching the handler. Added the missing fields to the test payloads.
- `UpdateUser_ValidRequest_Returns200`: same issue — `UpdateUserRequest` also requires `Email`, `MaxAccessLevel`, `IsActive`, `RoleIds`, all omitted. Added them.
- `GetAuditLogs_Returns200`: test hit `/api/audit-logs`, which doesn't exist; the real route is `GET /api/config/audit` (`ConfigController`). Fixed the test URL.

### accounts-service → 8/8 (was 5/8)
- **Real production bug**: `TransactionService.CreateTransactionAsync` called `db.Database.BeginTransactionAsync()` directly, which EF Core's `SqlServerRetryingExecutionStrategy` forbids (`InvalidOperationException: does not support user-initiated transactions`) — this would have crashed **every real deposit/withdrawal in production** whenever the retry-on-failure execution strategy is active, not just in tests. Fixed by wrapping the whole operation in `db.Database.CreateExecutionStrategy().ExecuteAsync(...)` (extracted the original body into a new private `CreateTransactionCoreAsync`).
- **Missing endpoint**: `GET /api/customers/{id}/balance` didn't exist anywhere in the API despite two tests depending on it. Added `ICustomerService.GetBalanceAsync` + `CustomerBalanceResponse(CreditLimit, CurrentBalance, AvailableCredit)` + a new `[HttpGet("{id:int}/balance")]` action on `CustomersController`.
- `GetAgentCustomers_ReturnsOnlyThatAgentsCustomers`: test hit `/api/agents/{id}/customers`, which doesn't exist; the real route is `GET /api/customers/by-agent/{agentId}`, which also returns a `PagedResult<T>` wrapper (`{ Items, TotalCount, Page, PageSize }`), not a flat array. Fixed the test URL and added a `PagedCustomers` wrapper record to deserialize into.
- `CreateTransaction_Deposit_Returns201AndUpdatesBalance`: test sent `Type = "Deposit"`, but `TransactionType` has no `Deposit` member (`Wire, Cash, Check, BankTransfer, FreePlay, CreditAdjustment, WagerSettlement, Reversal, CasinoAdjustment, ManualCorrection`) — silent enum-deserialization mismatch caused a 400. Changed the test to `Type = "Wire"`.

### lottery-service → 8/8 (was 2/8)
- `GetOpenDrawings_Returns200`: test hit a flat `/api/lottery/drawings?open=true` that doesn't exist; the real route is per-game: `GET /api/lottery/games/{id}/drawings`. Fixed the test URL (Pick3 always seeds as game ID 1).
- All 4 `PurchaseTicket_*` tests: request bodies used `DrawingId`, but `PurchaseRequest` requires `DrawingDetailId` — silently bound to `0`, so every purchase failed as "drawing not found." Also the required `DateToPlay` field was missing entirely. Fixed all 4 test payloads (plus the purchase call inside `GetCustomerTickets_Returns200`).
- **Real cross-service contract bug**: `LotteryService.Services.AccountsClient.GetBalanceAsync` deserialized a JSON `Balance` field from accounts-service's balance endpoint — but that endpoint (built this session, see accounts-service fixes above) returns `{ CreditLimit, CurrentBalance, AvailableCredit }`, no `Balance` key at all. In real production this would silently resolve every customer's balance to `0`, failing every lottery purchase with `INSUFFICIENT_BALANCE`. Fixed `AccountsClient`'s private `BalanceResponse` record to match the real shape and read `AvailableCredit` (the actual spending-power figure). Also stubbed `IAccountsClient` in `LotteryApiFactory` (`services.RemoveAll<IAccountsClient>(); services.AddSingleton<IAccountsClient>(new StubAccountsClient())`) so the integration tests don't depend on a live accounts-service.
- **Real product bug**: `LotteryTicket.Description` was `nvarchar(500)`, too narrow for a Pick4 boxed ticket — 24 unique permutation descriptions concatenated (`"Jul-16 Pick4 Drawing 1 BOX 1-2-3-4; ..."` × 24) blow past 500 chars, causing `SqlException: String or binary data would be truncated`. Widened to `nvarchar(4000)` in `LotteryDbContext.OnModelCreating` + new migration `20260716221027_WidenTicketDescription.cs`.

## Follow-up session (same day, after PR #1 merged) — betting-service and auth-service

`fix/integration-test-suite-bugs` was merged to `master`; this work continued on a new branch, `fix/betting-auth-integration-tests`.

### betting-service → 7/7 (was 0/14, contaminated result from earlier Docker instability)

A clean isolated run showed real failures, all self-inflicted from earlier fixes in *this same session*:

- **Real bug, introduced by me earlier today**: `BettingApiFactory.ResetDatabaseAsync()`'s `IDENTITY_INSERT` fix (see Bug 3 above) used `db.Database.BeginTransactionAsync()` directly — the exact same `SqlServerRetryingExecutionStrategy` incompatibility as the accounts-service production bug. `SeedGameAsync` and `SeedCustomerAsync` had the identical explicit-`Id`-without-`IDENTITY_INSERT` problem (`Games` and `Customers` tables) that hadn't surfaced yet because `ResetDatabaseAsync` was failing first. Fixed all three by wrapping each in `db.Database.CreateExecutionStrategy().ExecuteAsync(...)`.
- `HealthLive_Returns200` → 503: the `AddRedis(configuration.GetConnectionString("Redis"), "redis")` health check reads its connection string from static config at service-registration time, not from the fixture's ephemeral Testcontainers Redis instance — it was health-checking a Redis that doesn't exist in the test environment. A `ConfigureAppConfiguration` override attempt didn't take effect (ordering issue with `WebApplicationFactory`'s minimal-hosting interception, not fully diagnosed). Fixed reliably instead by removing the stale "redis" registration via `services.PostConfigure<HealthCheckServiceOptions>(...)` and re-adding it with the correct `_redis.GetConnectionString()`.
- `CancelWager_ExistingPendingWager_Returns200`: `WagersController.CancelWager` is intentionally `[HttpDelete]` → `NoContent()` (204), matching its own `[ProducesResponseType(StatusCodes.Status204NoContent)]` annotation. The test expected 200 — a test-side bug. Fixed the test (and renamed it `..._ReturnsNoContent`).

### auth-service — still blocked, confirmed environment-specific

Re-tested after the Docker Desktop restart (twice, including once more in this follow-up session): **still hangs identically** — SQL container health check never completes, CPU on the dotnet process goes flat within a few minutes and stays flat. This is the *only* one of 6 services that exhibits this behavior, across 4+ separate attempts including a full daemon restart, while every other service's Testcontainers-based SQL/Redis setup works reliably. Root cause not identified — something specific to `AuthApiFactory`'s container configuration or a Docker-level resource/state issue tied to it specifically. **Needs investigation outside of this session** (e.g. comparing `AuthApiFactory` against a working fixture line-by-line, or testing with Docker Desktop's resource limits raised).

## Final status (2026-07-16, end of day)

| Service | Unit tests | Integration tests |
|---|---|---|
| alerts-service | 19/19 | 8/8 |
| admin-service | 17/17 | 10/10 |
| accounts-service | 28/28 | 8/8 |
| lottery-service | 16/16 | 8/8 |
| betting-service | 41/41 | 7/7 |
| reports-service | 7/7 | no integration suite |
| auth-service | not re-verified this session | **blocked** — reproducible Testcontainers/Docker hang unique to this service, survives a full Docker Desktop restart |

Real production bugs found and fixed today (not test-only issues):
1. accounts-service: `TransactionService.CreateTransactionAsync` crashed on every real deposit/withdrawal (EF execution-strategy incompatibility)
2. accounts-service: `GET /api/customers/{id}/balance` never existed as an endpoint
3. lottery-service ↔ accounts-service: balance JSON contract mismatch — every real lottery purchase would see a $0 balance
4. lottery-service: `Description` column too narrow for Pick4 boxed tickets, causing real purchase failures
5. betting-service: same EF execution-strategy bug as #1, self-inflicted in this session's own test fixture (not present in production `WagerService` code — confirm no other `BeginTransactionAsync` usage exists in any `src/` folder before considering this fully closed)

---

## accounts-ui — Customers page fixed (2026-07-16, follow-up)

User-reported bug: after creating a customer, there was no way to see a customer list, or to modify/suspend one. Screenshots showed `/customers` rendering completely blank (no loading state, no error, nothing) for a logged-in `admin` user, even though creation and the individual customer detail page worked fine.

### Root cause
`CustomerListPage.tsx` only ever called `getCustomersByAgent(agentId)`, gated behind `enabled: agentId > 0`. An `admin` login is an Employee, not an Agent, so `agentId` is `null` from the JWT claims — the query silently never ran and nothing rendered, not even a loading/error state. There was also no `GET /api/customers` (list-all) endpoint on the backend at all — only `GET /api/customers/{id}`, `by-login/{loginName}`, and `by-agent/{agentId}` existed, so even a fixed frontend would have had nothing admin-appropriate to call.

Separately, `suspendCustomer`/`activateCustomer` API client functions already existed in `accountsApi.ts` but were never called from any page, and there was no `updateCustomer` function at all despite the backend's `UpdateCustomerAsync` being fully implemented — the "Personal" tab was read-only display, not a form.

### Fix
**Backend (`accounts-service`):**
- Added `ICustomerService.GetCustomersAsync(search, page, pageSize, ct)` + `CustomerService` impl — paginated, optional login-name search, no agent scoping.
- Added `GET /api/customers` on `CustomersController`, `[Authorize(Roles = "Admin,MasterAgent")]`.

**Frontend (`accounts-ui`):**
- `accountsApi.ts` — added `getCustomers(search, page, pageSize)` and `updateCustomer(id, request)`.
- `types/accounts.ts` — added `UpdateCustomerRequest`.
- `CustomerListPage.tsx` — branches on role: Admin/MasterAgent call `getCustomers` (with a search box, always enabled), Agent role still calls the scoped `getCustomersByAgent`. This is the actual fix for the blank page. Also added a "No customers found" empty state (previously an empty `data.items` array rendered a header-only table with no explanation).
- `PersonalTab.tsx` — rewritten from a static `<dl>` into an editable `react-hook-form` + zod form (alternate login, email, phone, odds format, instant-action toggle), mirroring the existing pattern in `LimitsTab.tsx`. Wired to `updateCustomer`.
- `CustomerDashboardPage.tsx` — added a Suspend/Activate button in the header, wired to the previously-unused `suspendCustomer`/`activateCustomer` calls. This is the "delete" equivalent — financial customer records aren't hard-deleted, matching the backend's existing status-toggle design (there is no delete endpoint anywhere in the API).

### Verification
Browser automation wasn't available in this environment (Chrome extension not connected), so verified via direct API calls against the running dev backend (real `CogAccounts` DB, not a test DB) instead:
- `GET /api/customers` → returns all 3 seeded/created customers including the one from the bug report screenshots (`donaldduck`)
- `GET /api/customers?search=donald` → correctly filters to 1 result
- `PUT /api/customers/3` → phone update persisted and reflected in a follow-up `GET`
- `POST /api/customers/3/suspend` → 204, status flips to `Suspended`; `POST .../activate` → 204, flips back to `Active`

`npx tsc --noEmit` clean on accounts-ui; `dotnet build` clean on accounts-service. Not re-run through the automated test suite (no test coverage exists for `CustomerListPage`/`PersonalTab`/`CustomerDashboardPage` yet — see "What's missing" list from the earlier recap).

---

## What was built / fixed in the 2026-08-30 session — full test run on a fresh machine, first-ever E2E pass

First time this repo's full test matrix (unit + integration + UI + Playwright E2E) was run on a brand-new machine with nothing pre-installed. Installed .NET 10 SDK, Node.js LTS, and confirmed Docker (Rancher Desktop) via winget/manual setup; started persistent `cog-sql` / `cog-redis` dev containers.

### Backend — 8 services, 170 unit + 58 integration tests, all passing

**auth-service integration tests unblocked** (was "reproducible hang, survives Docker Desktop restart" per 2026-07-16 notes): `auth-service/tests/AuthService.Tests.csproj` was still pinned to `Testcontainers.MsSql`/`Testcontainers.Redis` **3.7.0** — every other service had been bumped to **4.1.0** in July, but auth-service was missed. The 3.7.0 readiness-wait logic hangs indefinitely against this Docker setup even though the SQL container itself starts fine (verified via manual `sqlcmd`). Bumped both packages to 4.1.0 — 9/9 integration tests now pass in ~22s.

**betting-service startup bug (fresh-database bootstrap failure)**: `Program.cs`'s dev-migration block ran a legacy-compat `ExecuteSqlRawAsync` check (seeding `__EFMigrationsHistory` for DBs that predate EF migrations) *before* `MigrateAsync()`. On a genuinely fresh SQL Server (no `COGDB_Betting` database yet), that raw SQL fails with a login/database-not-found error, is swallowed by the surrounding try/catch, and `MigrateAsync()` — which would have created the database — never runs. Fixed by gating the legacy-compat block on `CanConnectAsync()` first.

### Frontend — 6 UIs + shared/ui-components, 75 unit tests, all passing

Fixed 3 real test failures surfaced on a fresh Node 24 run (none were environment flakiness):
- **admin-ui, accounts-ui**: `LoginPage` labels had no `htmlFor`/`id` association (`getByLabelText` couldn't resolve them) — a real accessibility bug, not just a test artifact. Fixed both, plus the same pattern in accounts-ui's `TransactionsTab` (Code/Type/Amount/Reference/Description fields) and `CreateCustomerPage`'s shared `FormField` helper (now auto-generates an id via `useId()` + `cloneElement`).
- **accounts-ui**: `TransactionsTab`'s history table never rendered the `description` column at all (schema had it, form collected it, table just didn't display it) — added the column.
- **casino-ui**: `CasinoLobbyPage.test.tsx` leaked a `vi.spyOn(axios, 'isAxiosError').mockReturnValue(true)` across tests (`vi.clearAllMocks()` doesn't undo a spy's return value, only `vi.restoreAllMocks()` does) — fixed the test, and separately hardened `sessionError.response?.status` → `sessionError?.response?.status` in `CasinoLobbyPage.tsx` since the component shouldn't crash on a null error regardless of the mock.
- **lottery-ui**: `GamesPage.test.tsx` asserted on post-load text synchronously instead of `await waitFor(...)`, so it ran before the mocked React Query promise resolved.

### E2E (Playwright) — first real run ever; CI has never executed these specs

Confirmed via `.github/workflows/frontend-apps.yml`: no CI job runs `npm run test:e2e` for either UI — these specs were written but never exercised against live services before today.

**accounts-ui** (21/21 passing): same label-association bugs above blocked essentially every test. Fixed; also fixed two test-side selector ambiguities (`getByText('Free Play')` matched both a balance-card label and a nav tab; `getByText(/wager limit/i)` matched both a form label and a transient "Loading wager limits…" string).

**betting-ui** (14/14 passing): the existing spec (`e2e/betting-flow.spec.ts`) referenced `data-testid` hooks (`bet-type-spread`, `side-home`, `win-amount`, `ticket-number`) and a bet-type/side picker step on the wager-entry page that **do not exist anywhere in the shipped UI** — zero `data-testid` attributes existed in the whole `betting-ui/src` tree. The real flow picks a specific line (type + side + price) directly from the game list via `handleSelectLine`, then enters customer/risk/wager-type on a single subsequent form. Rewrote the spec to match the actual implementation; added two `data-testid="sport-card"`/`"line-button"` hooks (non-behavioral) for stable selectors. Also found and fixed, in service of getting this spec green:
  - `betting-ui/playwright.config.ts` still pointed `baseURL`/`webServer.url` at `localhost:3000` — a leftover from before the April port-renumbering session; betting-ui has served on 5173 since then. This alone made the E2E suite unable to start at all.
  - **Real production bug**: `GamesController`'s `GET /api/games` (and `GetGameByIdAsync`) never returned line data at all — `CreateMap<GamePeriod, GamePeriodResponse>()` in `BettingMappingProfile` had no explicit binding from `GamePeriod.LineSet` (source) to `GamePeriodResponse.Lines` (destination); AutoMapper's convention matching doesn't connect differently-named members. Every game, forever, showed "Lines not yet available" in betting-ui regardless of whether lines had actually been set (verified: `PUT /api/lines/{id}/spread` wrote the data correctly; `GET /api/lines/{id}` returned it fine; only the games-list/detail endpoints silently dropped it). Fixed with `.ForMember(d => d.Lines, o => o.MapFrom(s => s.LineSet))`.
  - **Real production bug**: `LineSet.OfferingMoneyLine`/`OfferingTotal` (`shared/domain/Entities/LineSet.cs`) defaulted to `true` at the entity level, so a freshly created line row claimed to offer moneyline/total odds before anyone had ever set them — rendering broken `+null`/`null` buttons in betting-ui. Defaults changed to `false`; each `Set*Async` method already correctly flips its own flag to `true` when actually setting that market.
  - **Real production bug, the big one**: `POST /api/wagers` rejected `wagerType: "Straight"` — exactly the string the real React app sends — with a 400, because betting-service (unlike accounts-service, which got this fix back in April) never registered a global `JsonStringEnumConverter`. **Every wager submission through the real betting-ui was broken.** Same gap found in auth-service's `POST /api/users` (numeric enum required, not yet hit by a real frontend flow, so left as a noted finding rather than fixed). Fixed for betting-service in `ServiceCollectionExtensions.AddApplicationServices`.
  - **Real production bug**: after a successful wager submission, `WagerConfirmationPage` intermittently redirected back to `/sports` instead of landing on `/wagers/pending`. Root cause: a torn read between two independent reactive systems — the component's own empty-draft guard (`if (!customerId || items.length === 0) navigate('/sports')`) ran directly in the render body (not an effect), and `clearDraft()`'s synchronous Zustand notification could force a re-render of the still-mounted confirmation page *before* the component's own `mutation.isSuccess` had flushed, so the guard read stale "not submitted yet" state and fired. Fixed by tracking submission with a plain `useRef` (unaffected by cross-store render-order races, unlike `mutation.isSuccess`) and moving the actual `navigate()` call into a `useEffect`, eliminating both the race and a genuine `"Cannot update a component (BrowserRouter) while rendering a different component (WagerConfirmationPage)"` React warning. Confirmed via a standalone Playwright debug script capturing console/network/navigation events, since the failure symptom (landing on the wrong page) gave no direct signal about *why*.

None of these betting-service/betting-ui bugs were caught by the unit or integration test suites — all four were found only because this was the first time anyone ran the E2E spec against a live stack end-to-end.

### Seed data used for E2E (dev-only, not committed to `database/seed/`)
- `agent1` / `P@ssw0rd!` — provisioned as a `MasterAgent` in auth-service, accounts-service (`agent1`, id 1), and betting-service (id 1), via `POST /api/agents` (accounts-service) + `POST /api/users` (auth-service) + `POST /api/internal/agents` (betting-service) — accounts-service does **not** auto-provision new agents into auth-service (only into betting-service, via `BettingProvisioningService`); that's a manual step until/unless an `AuthProvisioningService` equivalent is added to accounts-service.
- `seedcust1` — one customer under `agent1` in both accounts-service and betting-service.
- One NFL game (Cowboys @ Eagles) with a spread line (`-3.5 / -110`) in betting-service, for the E2E line-picking flow.

### Full status at end of session

| Layer | Result |
|---|---|
| 8 backend services — unit tests | 170/170 |
| 6 backend services — integration tests (Testcontainers) | 58/58 |
| 6 UIs + shared/ui-components — unit tests | 75/75 |
| accounts-ui — Playwright E2E | 21/21 |
| betting-ui — Playwright E2E | 14/14 |

---

## Gap-list follow-up — same 2026-08-30 session, continued

Worked through the punch list left at the end of the full test run above: enum-converter gap, accounts-service→auth-service provisioning gap, missing reports/casino integration tests, thin accounts-ui unit coverage, and missing E2E specs for admin-ui/lottery-ui/reports-ui/casino-ui.

### Cross-cutting: `JsonStringEnumConverter` + FluentValidation auto-validation
Applied the same `AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))` fix (already done for accounts-service/betting-service in earlier sessions) to **auth-service, admin-service, alerts-service, lottery-service, reports-service, casino-service**. Also added `services.AddFluentValidationAutoValidation()` to auth-service, betting-service, casino-service — validators registered via `AddValidatorsFromAssemblyContaining<T>()` alone never actually ran without it (`casino-service`'s `Deposit_ZeroAmount` integration test caught this: returned 200 instead of 400).

This broadly-applied fix caused two regressions, both fixed same-session: `betting-service`'s `WagerResponse`-shaped integration test locals still expected numeric `Status`/`WagerType`; `lottery-service`'s `GameSummary` local still expected numeric `GameType`. **lottery-ui**'s frontend also silently broke (numeric `gameType`/`pickType` comparisons and `Record<number,string>` lookups) — caught by manually re-checking the frontend after the backend change, not by a failing test.

### accounts-service now auto-provisions agents into auth-service
New `AuthProvisioningService` (mirrors the existing `BettingProvisioningService` pattern): on `POST /api/agents`, after persisting the agent, accounts-service now also creates the corresponding auth-service user (`AgentType` → role name: `Master`→`MasterAgent`, `Agent`→`Agent`, `SubAgent`→`SubAgent`) with a generated temporary password, and returns it once in the response (`AgentResponse.TemporaryPassword`). Both provisioning calls (betting + auth) are individually try/caught so a downstream outage never turns a successful agent creation into a 500. accounts-ui's `CreateAgentModal` now shows a "Agent Created" confirmation panel with the login name and temporary password instead of closing immediately. 43/43 accounts-service unit tests (15 new).

### reports-service and casino-service integration tests (previously had none)
- **casino-service**: 16 new integration tests against a `CasinoApiFactory` with in-memory stub `ILiveDealerClient`/`IAccountsClient` (register, session, balance, deposit, withdraw, including rollback-on-external-failure paths). Needed `public partial class Program { }` (missing — blocked `WebApplicationFactory<Program>`), and the dev `SecretKey` placeholder fixed to match every other service's JWT key (a `ConfigureAppConfiguration` override was tried first and found unreliable for top-level-statement `Program.cs`, same known quirk as betting-service's Redis config in July — fixing the actual `appsettings.json` value is the reliable approach). 26/26 total.
- **reports-service**: 11 new integration tests. Since reports-service queries the *original 2014 legacy `COGDB` schema* directly via raw ADO.NET (stored procs `rptBetMakerActivity`/`ICBBSearchAgent`/`ICBBSearchCust`/`rptPackageTracker`, table `Customer`/`UpdatedCustomerTransaction`) — far too large to load into a Testcontainer — built a minimal synthetic schema exposing the same object names/columns, backed by dedicated `*Seed` tables. 18/18 total.

### accounts-ui unit test coverage
Added `customerListPage.test.tsx` (15 tests: role-scoping, search, pagination, states) and `personalTab.test.tsx` (8 tests); expanded `customerDashboard.test.tsx` from 4 to 15 tests (loading/not-found/tab-switching/suspend-activate). Along the way fixed the same missing-`htmlFor` bug in `PersonalTab`'s `Field` helper (now `useId()` + `cloneElement`, matching the pattern already used elsewhere).

### E2E specs — admin-ui, lottery-ui, reports-ui (casino-ui explicitly skipped, see below)
All three follow the same pattern established for accounts-ui/betting-ui: since none of these apps have real login UI wired to a running auth flow in dev (admin-ui does; lottery-ui/reports-ui read a bare `accessToken` from `localStorage` and assume SSO), a `loginAs()` helper mints a real JWT via `page.request.post()` against auth-service and seeds it with `page.addInitScript()` before navigating.

- **admin-ui** (20/20): fixed missing `htmlFor`/`id` pairs across `UsersPage`, `RolesPage`, `SystemConfigPage` modals.
- **lottery-ui** (10/10): found and fixed real production bugs while building this spec — see "critical production bugs" below.
- **reports-ui** (12/12): built a real (non-ephemeral) `COGDB` dev database in the `cog-sql` container with the same synthetic schema used for reports-service's integration tests, seeded with an `agent1`/`e2eplayer1` fixture, and verified every report endpoint via direct `Invoke-RestMethod` calls before writing the spec. Fixed the same missing-`htmlFor` bug across `WagersReport`/`TransactionsReport`/`AgentsReport`. Two selector ambiguities caught the substring-matching gotcha the hard way: `getByLabel('To')` also matched "Cus**to**mer ID (0 = all)", and `getByText('agent1')` also matched "agent1@cog.local" — both fixed with `{ exact: true }`.

**Latent bug found across admin-ui, lottery-ui, reports-ui**: none of the three excluded `e2e/**` from Vitest's own test collection, so `npm test` tried to run the Playwright spec file as a Vitest suite and failed with `Playwright Test did not expect test.describe() to be called here` — this had been silently broken since each app's E2E spec was first added (nobody had re-run `npm test` afterward). accounts-ui and betting-ui already had the right `exclude: ['**/node_modules/**', '**/e2e/**']`; added the same to all three.

### lottery-service — critical production bugs found via the E2E work (would never have been caught by unit/integration tests alone)
- `TicketsController.Purchase`/`GetMyTickets` read `User.FindFirstValue("customerId")` — **a claim that no real JWT has ever contained** (real claims are `sub`/`domain_id`/etc., per auth-service's actual token issuance). Every real purchase attempt failed. Fixed to read `domain_id`; made the optional `agentId` claim non-blocking via `TryParse` instead of a hard requirement.
- `AccountsClient.GetBalanceAsync` called accounts-service's `[Authorize]`-protected balance endpoint **with no bearer token**, silently swallowed the resulting 401, and returned `0m` — every purchase failed with "Insufficient balance: Available $0.00" regardless of real balance. Fixed by forwarding the incoming request's `Authorization` header via `IHttpContextAccessor`.
- Same fresh-database bootstrap bug as betting-service (legacy-compat raw SQL ran before `MigrateAsync()`, swallowed the resulting error on a truly fresh DB) — same `CanConnectAsync()` gating fix.
- `database/seed/04_lottery_games.sql` had 5 occurrences of invalid `datetime2 + time` SQL Server syntax — fixed with the `DATEADD(DAY, DATEDIFF(DAY, 0, @date), CAST('HH:MM:SS' AS DATETIME2))` idiom.

### casino-ui — real bugs found, E2E explicitly out of scope
`LoginPage.tsx` posts to `/api/auth/login`, but casino-ui's `vite.config.ts` proxy only had a catch-all `/api` → casino-service (port 5080) rule — **casino-service has no `/api/auth/*` endpoints at all**, so login could never succeed in dev. Fixed by adding a `/api/auth` → auth-service (5010) proxy rule ahead of the catch-all, matching accounts-ui's/admin-ui's existing pattern. Also fixed two latent field-name mismatches masked by the routing bug: the login request sent `{ username, password }` (auth-service requires `loginName`), and the response type expected a `customerId` field that doesn't exist on `AuthTokenResponse` (real field is `domainEntityId`); the failed-login handler read `err.response.data.error`, but auth-service's `Unauthorized` response is a `ProblemDetails` object (`{ title, detail }`, no `error` key) — fixed to read `.detail`. None of this was caught by `LoginPage.test.tsx` since it mocks `axios` entirely, so a wrong field name only ever manifests against a real backend. 11/11 casino-ui unit tests still pass after the fix.

**casino-ui E2E was deliberately not written this session**: `casino-service`'s dev `appsettings.json` points `Register`/`Deposit`/`Withdraw` at the *real* external Live Dealer vendor (`https://ittds.newland.cr`) — there is no local stub outside the Testcontainers integration-test project (which uses `StubLiveDealerClient`). Asked the user how to proceed rather than risk sending real outbound calls to third-party production infrastructure from an automated test run; user chose to skip casino-ui E2E for now. **Follow-up needed**: either a local mock/WireMock stand-in for the vendor API in dev config, or an explicit sanctioned test account, before casino-ui can get real E2E coverage.

### Full status at end of this follow-up
| Layer | Result |
|---|---|
| accounts-service unit tests | 43/43 (was 28/28) |
| casino-service tests (unit + integration) | 26/26 (was 10/10, no integration) |
| reports-service tests (unit + integration) | 18/18 (was 7/7, no integration) |
| accounts-ui unit tests | +38 new (customerListPage, personalTab, expanded customerDashboard) |
| admin-ui — Playwright E2E | 20/20 (new) |
| lottery-ui — Playwright E2E | 10/10 (new) |
| reports-ui — Playwright E2E | 12/12 (new) |
| casino-ui — Playwright E2E | not written (see above) |

---
