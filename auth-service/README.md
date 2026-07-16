# auth-service

JWT authentication and user management service for the COG platform. Issues short-lived access tokens (15 min) and long-lived refresh tokens (7 days, stored in Redis). Replaces the legacy Delphi GUID session system and BitPermission bitmask authorization.

**Port:** `5010` (http) / `5011` (https)  
**Database:** `COGDB_Auth` (SQL Server)  
**Cache:** Redis (refresh token storage)

---

## API Endpoints

### Authentication

| Method | Route | Auth | Description |
|---|---|---|---|
| `POST` | `/api/auth/login` | None | Authenticate and receive access + refresh tokens |
| `POST` | `/api/auth/refresh` | None | Rotate refresh token and get new access token |
| `POST` | `/api/auth/logout` | None | Revoke refresh token |
| `GET` | `/api/auth/me` | Bearer | Get current user info from token |

### User Management

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/api/users` | Bearer | List all users (paginated) |
| `POST` | `/api/users` | Bearer | Create a new user |
| `GET` | `/api/users/{id}` | Bearer | Get user by ID |
| `PATCH` | `/api/users/{id}` | Bearer | Update user (partial) |
| `DELETE` | `/api/users/{id}` | Bearer | Deactivate user (soft delete) |
| `POST` | `/api/users/{id}/roles` | Bearer | Assign roles to user |

### Health

| Method | Route | Description |
|---|---|---|
| `GET` | `/health/live` | Liveness probe |
| `GET` | `/health/ready` | Readiness probe (checks DB + Redis) |

---

## Token Structure

**Access token** (JWT, 15 min):
```json
{
  "sub": "42",
  "name": "agent1",
  "roles": ["Agent", "TicketWriter"],
  "domainEntityId": 7,
  "iss": "cog-auth-service",
  "aud": "cog-services"
}
```

**Refresh token**: opaque UUID stored in Redis with TTL = 7 days. Single-use — each refresh rotates to a new token.

---

## Configuration

`src/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=COGDB_Auth;...",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "SecretKey": "CHANGE_THIS_IN_PRODUCTION_MIN_32_CHARS",
    "Issuer": "cog-auth-service",
    "Audience": "cog-services",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  }
}
```

| Variable | Description |
|---|---|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `ConnectionStrings__Redis` | Redis connection string |
| `Jwt__SecretKey` | HMAC-SHA256 signing key — see **JWT Secret Key** section below |
| `Jwt__AccessTokenExpiryMinutes` | Access token TTL (default: 15) |
| `Jwt__RefreshTokenExpiryDays` | Refresh token TTL (default: 7) |

In production these are injected via Kubernetes secrets (see `k8s/deployment.yaml`).

### JWT Secret Key

**Requirements:** minimum 32 bytes (256 bits) for HMAC-SHA256. The same key must be used by **all services** that validate tokens (auth-service signs, all others validate).

**Generate a key:**
```powershell
# PowerShell
[Convert]::ToBase64String((1..32 | ForEach-Object { [byte](Get-Random -Max 256) }))
```

**Local dev** — set in `appsettings.Development.json` (never committed to source control):
```json
{
  "Jwt": {
    "SecretKey": "6UZhMYYN1EqSmi7rtNvpRBS46WsNbVuYZwXzNft7h8c="
  }
}
```

**Production** — store in AWS Secrets Manager and inject as `Jwt__SecretKey` environment variable. Never put the real key in any `appsettings.json` file.

---

## Local Development

### Prerequisites
- .NET 10 SDK
- SQL Server (local or Docker)
- Redis (local or Docker)

### Run
```bash
# Start dependencies
docker run -d -e ACCEPT_EULA=Y -e "SA_PASSWORD=Dev_Password123!" \
  -p 1433:1433 --name cog-sql mcr.microsoft.com/mssql/server:2022-latest
docker run -d -p 6379:6379 --name cog-redis redis:alpine

# Configure local connection string (uses SQL auth, not Windows auth)
# src/appsettings.Development.json:
# "DefaultConnection": "Server=localhost,1433;Database=COGDB_Auth;User Id=sa;Password=Dev_Password123!;TrustServerCertificate=True;"

# Apply migrations (seeds roles + default admin user)
cd src
dotnet ef database update

# Start service
dotnet run --launch-profile http
# Listening on http://localhost:5010
```

### Ports

| Profile | URL |
|---|---|
| `http` | `http://localhost:5010` |
| `https` | `https://localhost:5011` |

### Swagger UI

Open `http://localhost:5010/swagger`.

To test protected endpoints:

1. Expand `POST /api/auth/login` → **Try it out** → send:
   ```json
   { "loginName": "admin", "password": "Admin123!" }
   ```
2. From the response body, copy **only** the `accessToken` value — the long `eyJ...` string. Stop before the closing `"`. Do **not** copy `refreshToken` or any surrounding JSON.
3. Click **Authorize** (top right), paste **just the token** (no `Bearer ` prefix — Swagger adds it automatically with the `Http` scheme).

> **Common mistake:** pasting the entire JSON response instead of just the token value causes `invalid_token` errors.

The default seed admin account (`admin` / `Admin123!`) is created by the `SeedAdminUser` migration and assigned the `Admin` role.

### Known Issues

| Symptom | Cause | Fix |
|---|---|---|
| `dotnet run` hangs silently | SQL Server or Redis unreachable on startup | Start Docker containers first; migration errors are caught and logged — service still starts |
| `Cannot generate SSPI context` | `Trusted_Connection=True` fails Windows auth against localhost | Use SQL auth (`User Id=sa;Password=...`) in `appsettings.Development.json` |
| `PendingModelChangesWarning` on `dotnet ef database update` | Model changed since last migration | Run `dotnet ef migrations add <Name>` before updating |
| `Violation of PRIMARY KEY` on migration | Duplicate seed data in a new migration | Remove `InsertData` calls that duplicate rows already inserted by an earlier migration |
| Port already in use | Previous `dotnet run` process still alive | Kill it: `taskkill /F /IM dotnet.exe` (Windows) or `pkill dotnet` (Linux/Mac) |

---

## Testing

### Unit tests
```bash
dotnet test tests/ --filter "Category!=Integration"
```

### Integration tests
Require Docker (Testcontainers spins up SQL Server 2022 + Redis 7 containers automatically):
```bash
dotnet test tests/ --filter "Category=Integration"
# or run everything:
dotnet test tests/
```

Coverage:
- `LoginAsync` — valid credentials, bad password, locked account
- `RefreshAsync` — token rotation, reuse detection (replay attack)
- `LogoutAsync` — token revocation
- `GetCurrentUser` — authenticated / unauthenticated

---

## Docker

```bash
# Build (run from modernization/ root — Dockerfile copies shared/domain)
docker build -f auth-service/Dockerfile -t cog/auth-service:local .

# Run
docker run -p 5001:8080 \
  -e ConnectionStrings__DefaultConnection="..." \
  -e ConnectionStrings__Redis="..." \
  -e Jwt__SecretKey="..." \
  cog/auth-service:local
```

The image uses a non-root user (`appuser`) and exposes port `8080` internally.

---

## Kubernetes

Manifests are in `k8s/`:
- `deployment.yaml` — 2 replicas, resource limits, liveness/readiness probes
- `service.yaml` — ClusterIP service
- `hpa.yaml` — Horizontal Pod Autoscaler (2–10 replicas on CPU)

Secrets are read from the `cog-auth-secret` Kubernetes Secret (provisioned by Terraform via AWS Secrets Manager).

---

## Architecture Notes

- **Soft deletes** — users are never hard-deleted; `IsActive = false` blocks login.
- **Role-based claims** — roles are embedded in the JWT so downstream services can authorize without a DB call.
- **Refresh token rotation** — every refresh issues a new token and invalidates the old one. Reuse of a revoked token triggers revocation of the entire token family (compromise detection).
- **Password hashing** — BCrypt with cost factor 12.
- **Migrations** — EF Core migrations under `src/Migrations/`. Run `dotnet ef migrations add <Name>` from `src/` to add new migrations.
