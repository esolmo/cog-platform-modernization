# accounts-service

Customer account and financial transaction management service for the COG platform. Handles the agent hierarchy, customer profiles, credit limits, and transaction ledger. Replaces the Accounts Delphi desktop (56 forms) and the `Cog-web-betting/Accounts/` ASP pages.

**Port:** `5020` (http) / `5021` (https)  
**Database:** `CogAccounts` (SQL Server)

---

## API Endpoints

### Customers

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/api/customers` | Bearer | List customers for the authenticated agent (paginated) |
| `POST` | `/api/customers` | Bearer | Create a new customer |
| `GET` | `/api/customers/{id}` | Bearer | Get customer with balance and limits |
| `PATCH` | `/api/customers/{id}` | Bearer | Update customer profile (partial) |
| `DELETE` | `/api/customers/{id}` | Bearer | Deactivate customer (soft delete) |

### Transactions

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/api/transactions/{customerId}` | Bearer | List transactions for a customer (paginated) |
| `POST` | `/api/transactions` | Bearer | Post a new transaction (credit or debit) |

### Balances

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/api/customers/{id}/balance` | Bearer | Get current balance summary |

### Agents

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/api/agents` | Bearer | Get agent hierarchy (sub-agents) |
| `GET` | `/api/agents/{id}/customers` | Bearer | Get customers for a specific agent |

### Health

| Method | Route | Description |
|---|---|---|
| `GET` | `/health/live` | Liveness probe |
| `GET` | `/health/ready` | Readiness probe |

---

## Transaction Types

| Code | Types |
|---|---|
| `Credit` | Cash, Wire, Check, BankTransfer, FreePlay, CreditAdjustment, ManualCorrection |
| `Debit` | Cash, Wire, Check, BankTransfer, WagerLoss, ManualCorrection |

Each transaction records the balance before and after, the agent who entered it, and an optional reference/description.

---

## Configuration

`src/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=COGDB_Accounts;..."
  },
  "Jwt": {
    "SecretKey": "CHANGE_ME_IN_PRODUCTION_USE_A_LONG_RANDOM_SECRET",
    "Issuer": "cog-auth-service",
    "Audience": "cog-services"
  }
}
```

| Variable | Description |
|---|---|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `Jwt__SecretKey` | **Must match auth-service exactly** — this service validates tokens but does not sign them |

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
# Start dependencies (if not already running)
docker run -d -e ACCEPT_EULA=Y -e "SA_PASSWORD=Dev_Password123!" \
  -p 1433:1433 --name cog-sql mcr.microsoft.com/mssql/server:2022-latest

cd src
dotnet ef database update
dotnet run --launch-profile http
# Listening on http://localhost:5020
```

### Ports

| Profile | URL |
|---|---|
| `http` | `http://localhost:5020` |
| `https` | `https://localhost:5021` |

### Swagger UI

Open `http://localhost:5020/swagger`.

To test protected endpoints:
1. Get a token from auth-service: `POST http://localhost:5010/api/auth/login` with `{ "loginName": "admin", "password": "Admin123!" }`
2. Copy **only** the `accessToken` value (the `eyJ...` string — do not include `refreshToken` or surrounding JSON)
3. Click **Authorize** in Swagger and paste just the token (no `Bearer ` prefix)

---

## Testing

### Unit tests
```bash
dotnet test tests/ --filter "Category!=Integration"
```

### Integration tests (requires Docker)
```bash
dotnet test tests/
```

Key integration test coverage:
- Get customer — found / not found
- Get balance — returns credit limit and current balance
- Create customer — valid, duplicate login name (409)
- Post transaction — deposit updates balance correctly
- Get customers scoped to agent — no cross-agent data leakage

---

## Docker

```bash
# Build from modernization/ root
docker build -f accounts-service/Dockerfile -t cog/accounts-service:local .

docker run -p 5002:8080 \
  -e ConnectionStrings__DefaultConnection="..." \
  -e Jwt__Key="..." \
  cog/accounts-service:local
```

---

## Architecture Notes

- **Agent hierarchy** — agents form a self-referencing tree (`Agent.ParentAgentId`). Customers belong to a leaf agent. The `GetSubAgentHierarchy` query replaces the legacy `fn_GetSubAgentHierarchyByID` recursive CTE stored procedure, now expressed as an EF Core recursive query.
- **Balance invariant** — `CustomerBalance.CurrentBalance` is always updated atomically within the same transaction as the `CustomerTransaction` insert (EF Core `SaveChanges` wraps both in a DB transaction).
- **Soft deletes** — customers and agents set `IsActive = false`; they are excluded from queries but retained for audit purposes.
- **Column name overrides** — primary keys use legacy names (`idAgent`, `idCustomer`) to ease SQL-level migration validation against the original schema.
- **Free plays** — tracked separately in `CustomerBalance.FreePlayBalance`; free-play transactions use code `Credit` / type `FreePlay`.
