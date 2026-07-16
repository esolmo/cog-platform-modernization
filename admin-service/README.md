# admin-service

System administration API for the COG platform. Manages application users, roles, permissions, system configuration, and audit logs. Replaces the Admin Delphi desktop and the LoginsRoles Delphi desktop.

**Port:** `5030` (http) / `5031` (https)  
**Database:** `COGDB_Admin` (SQL Server)

---

## API Endpoints

### Users

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/api/users` | Bearer | List all users (paginated) |
| `POST` | `/api/users` | Bearer | Create a new application user |
| `GET` | `/api/users/{id}` | Bearer | Get user by ID |
| `PATCH` | `/api/users/{id}` | Bearer | Update user (partial) |
| `DELETE` | `/api/users/{id}` | Bearer | Deactivate user (soft delete) |

### Roles

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/api/roles` | Bearer | List all roles with permissions |
| `POST` | `/api/roles` | Bearer | Create a new role |
| `PUT` | `/api/roles/{id}/permissions` | Bearer | Update role permissions |

### System Configuration

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/api/config` | Bearer | Get all system configuration values |
| `PUT` | `/api/config/{key}` | Bearer | Update a configuration value |

### Audit Logs

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/api/audit-logs` | Bearer | Query audit log (filterable by user, entity, date range) |

### Health

| Method | Route | Description |
|---|---|---|
| `GET` | `/health/live` | Liveness probe |
| `GET` | `/health/ready` | Readiness probe |

---

## Seeded Roles

| Role | Description | Legacy Equivalent |
|---|---|---|
| SuperAdmin | Full system access | SuperAdmin |
| Admin | User + config management | Admin |
| LinesManager | Game and line management | LinesManager |
| TicketWriter | Wager entry | Agent |
| AgentManager | Agent hierarchy management | AgentManager |
| ReportsViewer | Read-only reports access | ReportsViewer |

---

## Configuration

`src/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=COGDB_Admin;..."
  },
  "Jwt": {
    "SecretKey": "CHANGE_ME_IN_PRODUCTION_USE_A_LONG_RANDOM_SECRET",
    "Issuer": "cog-auth-service",
    "Audience": "cog-services"
  },
  "AllowedOrigins": [
    "http://localhost:5173",
    "http://localhost:5175"
  ]
}
```

### JWT Secret Key

This service only **validates** tokens issued by auth-service. The `Jwt__SecretKey` must be identical to the one configured in auth-service.

**Local dev** — set in `appsettings.Development.json`:
```json
{
  "Jwt": {
    "SecretKey": "6UZhMYYN1EqSmi7rtNvpRBS46WsNbVuYZwXzNft7h8c="
  }
}
```

**Production** — inject as `Jwt__SecretKey` environment variable from AWS Secrets Manager (same secret as auth-service). Never commit real keys to source control.

---

## Local Development

```bash
cd src
dotnet ef database update
dotnet run --launch-profile http
# Listening on http://localhost:5030
```

### Ports

| Profile | URL |
|---|---|
| `http` | `http://localhost:5030` |
| `https` | `https://localhost:5031` |

### Swagger UI

Open `http://localhost:5030/swagger`.

To test protected endpoints:
1. Get a token from auth-service: `POST http://localhost:5010/api/auth/login` with `{ "loginName": "admin", "password": "Admin123!" }`
2. Copy **only** the `accessToken` value (the `eyJ...` string — do not include `refreshToken` or surrounding JSON)
3. Click **Authorize** in Swagger and paste just the token (no `Bearer ` prefix)

---

## Testing

### Integration tests (requires Docker)
```bash
dotnet test tests/
```

Coverage:
- `GET /api/users` — returns seeded users
- `POST /api/users` — valid (201), duplicate username (409)
- `GET /api/users/{id}` — found (200), not found (404)
- `PATCH /api/users/{id}` — returns 200
- `GET /api/roles` — returns ≥6 seeded roles
- `GET /api/config` — returns 200
- `GET /api/audit-logs` — returns 200

---

## Docker

```bash
docker build -f admin-service/Dockerfile -t cog/admin-service:local .

docker run -p 5030:8080 \
  -e ConnectionStrings__DefaultConnection="..." \
  -e Jwt__SecretKey="..." \
  cog/admin-service:local
```

---

## Known Issues

| Issue | Cause | Fix |
|---|---|---|
| `IDX10517 The signature key was not found` | .NET 10 JWT bearer defaults to `JsonWebTokenHandler`, which rejects tokens without a `kid` header | Fixed — `UseSecurityTokenValidators = true` in `AddJwtBearer` forces legacy `JwtSecurityTokenHandler` |
| `Cannot generate SSPI context` | `Trusted_Connection=True` fails on localhost SQL Server | Use SQL auth in `appsettings.Development.json`: `User ID=sa;Password=...` |

---

## Architecture Notes

- **Immutable audit log** — `AuditLogs` records `OldValues` and `NewValues` as JSON snapshots. Records are never updated or deleted. The `bigint` PK accommodates high-volume write scenarios.
- **Soft deletes** — `ApplicationUser.IsActive = false` blocks login in `auth-service` without losing historical data.
- **`LegacyBitValue`** — each `Permission` row stores the original bitmask integer from `Constants.asp` to facilitate parallel-run validation against the legacy system.
- **System configuration** — key/value pairs in `SystemConfigurations` replace the hard-coded constants in `Constants.asp` and the Delphi server config file. Changes are applied without a service restart.
