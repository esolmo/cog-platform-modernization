# lottery-service

Pick3 and Pick4 lottery ticket service for the COG platform. Handles drawing management, ticket purchase, and combinatorial pick expansion (boxed picks). Replaces the `Lottery` ASP.NET WebForms application and its LINQ-to-SQL data layer.

**Port:** `5060` (http) / `5061` (https)  
**Database:** `COGDB_Lottery` (SQL Server)

---

## API Endpoints

### Games

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/api/lottery/games` | Bearer | List available lottery games (Pick3, Pick4) |

### Drawings

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/api/lottery/drawings` | Bearer | List drawings (`?open=true` for open only) |
| `POST` | `/api/lottery/drawings` | Bearer | Create a new drawing |

### Tickets

| Method | Route | Auth | Description |
|---|---|---|---|
| `POST` | `/api/lottery/tickets` | Bearer | Purchase a ticket |
| `GET` | `/api/lottery/tickets/{customerId}` | Bearer | Get all tickets for a customer |
| `GET` | `/api/lottery/tickets/{customerId}/{ticketId}` | Bearer | Get ticket detail |

### Health

| Method | Route | Description |
|---|---|---|
| `GET` | `/health/live` | Liveness probe |
| `GET` | `/health/ready` | Readiness probe |

---

## Pick Types

| Code | Name | Description |
|---|---|---|
| `1` | Straight | Exact match required. 1 entry per pick |
| `2` | Boxed | Any order wins. Expands to all permutations |

### Boxed pick expansion

| Game | Numbers | Permutations | Amount split |
|---|---|---|---|
| Pick3 | 3 unique digits | 3! = 6 | `amount / 6` per entry |
| Pick4 | 4 unique digits | 4! = 24 | `amount / 24` per entry |

The expansion is handled by the `Permutations<T>` class (ported from `Facet.Combinatorics`) in `src/Combinatorics/`.

---

## Configuration

`src/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=COGDB_Lottery;..."
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
dotnet ef database update
# Seeds Pick3 (Id=1) and Pick4 (Id=2) game records automatically

dotnet run --launch-profile http
# Listening on http://localhost:5060
```

### Ports

| Profile | URL |
|---|---|
| `http` | `http://localhost:5060` |
| `https` | `https://localhost:5061` |

### Swagger UI

Open `http://localhost:5060/swagger`.

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

Testcontainers starts SQL Server 2022. `LotteryApiFactory` calls `MigrateAsync()` on each test run, which seeds Pick3 + Pick4 games.

Key coverage:
- `GET /api/lottery/games` — returns ≥2 seeded games
- `GET /api/lottery/drawings?open=true`
- `POST /api/lottery/tickets` — straight Pick3 (1 entry), boxed Pick3 (6 entries × `amount/6`), boxed Pick4 (24 entries × `amount/24`)
- `POST /api/lottery/tickets` on closed drawing — returns 422
- `GET /api/lottery/tickets/{customerId}`

---

## Docker

```bash
docker build -f lottery-service/Dockerfile -t cog/lottery-service:local .

docker run -p 5060:8080 \
  -e ConnectionStrings__DefaultConnection="..." \
  -e Jwt__SecretKey="..." \
  cog/lottery-service:local
```

---

## Known Issues

| Issue | Cause | Fix |
|---|---|---|
| `IDX10517 The signature key was not found` | .NET 10 JWT bearer defaults to `JsonWebTokenHandler`, which rejects tokens without a `kid` header | Fixed — `UseSecurityTokenValidators = true` in `AddJwtBearer` forces legacy `JwtSecurityTokenHandler` |
| `Cannot generate SSPI context` | `Trusted_Connection=True` fails on localhost SQL Server | Use SQL auth in `appsettings.Development.json`: `User ID=sa;Password=...` |

---

## Architecture Notes

- **Combinatorics** — `src/Combinatorics/Permutations.cs` is a self-contained port of `Facet.Combinatorics`. It generates all permutations lazily without materialising the full list upfront.
- **Closed drawing guard** — `POST /api/lottery/tickets` checks `Drawing.IsOpen` and `Drawing.CutoffTime` before inserting. Returns HTTP 422 with a problem detail if the drawing is not accepting tickets.
- **Balance check** — ticket purchase calls `accounts-service` to verify the customer has sufficient available credit before inserting the ticket.
- **Number4 default** — `LotteryPickEntries.Number4` defaults to `0` for Pick3 entries to keep a single table structure for both game types.
