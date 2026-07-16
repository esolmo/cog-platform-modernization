# reports-service

Reporting API for the COG platform. Provides wager activity, transaction, and agent performance reports. Replaces the `WebReports` ASP.NET WebForms application.

**Port:** `5070` (http) / `5071` (https)  
**Database:** `COGDB` (SQL Server, read-only reporting queries)

---

## API Endpoints

### Reports

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/api/reports/wagers` | Bearer | Wager activity report (`?agentId=N&from=&to=`) |
| `GET` | `/api/reports/transactions` | Bearer | Transaction report (`?agentId=N&from=&to=`) |
| `GET` | `/api/reports/agents` | Bearer | Agent performance report (`?from=&to=`) |

All report endpoints accept `from` and `to` ISO 8601 date parameters and return paginated JSON.

### Health

| Method | Route | Description |
|---|---|---|
| `GET` | `/health/live` | Liveness probe |
| `GET` | `/health/ready` | Readiness probe |

---

## Configuration

`src/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=COGDB;..."
  },
  "Jwt": {
    "SecretKey": "CHANGE_ME_IN_PRODUCTION_USE_A_LONG_RANDOM_SECRET",
    "Issuer": "cog-auth-service",
    "Audience": "cog-services"
  }
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
dotnet run --launch-profile http
# Listening on http://localhost:5070
# No migrations — reporting service uses read-only cross-service queries
```

### Ports

| Profile | URL |
|---|---|
| `http` | `http://localhost:5070` |
| `https` | `https://localhost:5071` |

### Swagger UI

Open `http://localhost:5070/swagger`.

To test protected endpoints:
1. Get a token from auth-service: `POST http://localhost:5010/api/auth/login` with `{ "loginName": "admin", "password": "Admin123!" }`
2. Copy **only** the `accessToken` value (the `eyJ...` string — do not include `refreshToken` or surrounding JSON)
3. Click **Authorize** in Swagger and paste just the token (no `Bearer ` prefix)

---

## Testing

```bash
dotnet test tests/
```

---

## Docker

```bash
docker build -f reports-service/Dockerfile -t cog/reports-service:local .

docker run -p 5070:8080 \
  -e ConnectionStrings__DefaultConnection="..." \
  -e Jwt__SecretKey="..." \
  cog/reports-service:local
```

---

## Known Issues

| Issue | Cause | Fix |
|---|---|---|
| `IDX10517 The signature key was not found` | .NET 10 JWT bearer defaults to `JsonWebTokenHandler`, which rejects tokens without a `kid` header | Fixed — `UseSecurityTokenValidators = true` in `AddJwtBearer` forces legacy `JwtSecurityTokenHandler` |
| `Cannot generate SSPI context` | `Trusted_Connection=True` fails on localhost SQL Server | Use SQL auth in `appsettings.Development.json`: `User ID=sa;Password=...` |

---

## Architecture Notes

- **Read-only** — `reports-service` has no EF Core `DbContext` migrations. It runs raw SQL queries against the shared COGDB read replica (or the primary in dev). This mirrors the legacy approach where WebReports used `DataSet`-based ADO.NET queries.
- **No write operations** — the service has no `POST`/`PUT`/`DELETE` endpoints. It is a pure reporting read path and can safely connect to an RDS read replica.
- **Parameterized queries** — all SQL is parameterized (`SqlParameter`) to prevent SQL injection. No string concatenation for user-supplied filter values.
- **Pagination** — all report endpoints use `OFFSET / FETCH NEXT` SQL pagination and return `{ items, page, pageSize, totalCount }`.
