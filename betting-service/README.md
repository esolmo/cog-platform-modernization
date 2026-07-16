# betting-service

Core revenue path service for the COG platform. Manages wager creation, game/line management, and sports data. Replaces the BetMaker Delphi desktop client, the Cog-web-betting Classic ASP pages, and the LinesManager desktop.

**Port:** `5050` (http) / `5051` (https)  
**Database:** `COGDB_Betting` (SQL Server)  
**Cache:** Redis (odds cache, idempotency)

---

## API Endpoints

### Wagers

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/api/wagers` | Bearer | List wagers (filterable by customer, date, status) |
| `POST` | `/api/wagers` | Bearer | Create a new wager |
| `GET` | `/api/wagers/{id}` | Bearer | Get wager detail |
| `DELETE` | `/api/wagers/{id}` | Bearer | Cancel a pending wager |

### Games

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/api/games` | Bearer | List games (filterable by sport, date) |
| `GET` | `/api/games/{id}` | Bearer | Get game with periods and current lines |
| `GET` | `/api/games/{id}/lines` | Bearer | Get all lines for a game |

### Lines

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/api/lines/{gameId}` | Bearer | Get current lines for a game |
| `PUT` | `/api/lines/{gameId}/spread` | Bearer | Update spread line |
| `PUT` | `/api/lines/{gameId}/total` | Bearer | Update total (over/under) line |
| `PUT` | `/api/lines/{gameId}/moneyline` | Bearer | Update money line |

### Sports

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/api/sports` | Bearer | List all active sport types |

### Health

| Method | Route | Description |
|---|---|---|
| `GET` | `/health/live` | Liveness probe |
| `GET` | `/health/ready` | Readiness probe (checks DB + Redis) |

---

## Wager Types

| Type | Description |
|---|---|
| Straight | Single-game spread, total, or moneyline bet |
| Parlay | 2–10 game accumulator; all legs must win |
| Teaser | Parlay with adjusted spreads/totals in the bettor's favour |
| If-Bet | Conditional straight bet — second leg only placed if first wins |
| Reverse | Two mirrored if-bets |

---

## Configuration

`src/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=COGDB_Betting;...",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "SecretKey": "...",
    "Issuer": "cog-auth-service",
    "Audience": "cog-services",
    "AccessTokenExpiryMinutes": 15
  }
}
```

| Variable | Description |
|---|---|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `ConnectionStrings__Redis` | Redis connection string |
| `Jwt__SecretKey` | Same key as auth-service (token validation only) |

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

### Prerequisites
- .NET 10 SDK
- SQL Server
- Redis

### Run
```bash
# Apply migrations
cd src
dotnet ef database update

# Start service
dotnet run --launch-profile http
# Listening on http://localhost:5050
```

### Ports

| Profile | URL |
|---|---|
| `http` | `http://localhost:5050` |
| `https` | `https://localhost:5051` |

### Swagger UI

Open `http://localhost:5050/swagger`.

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
- Create wager — valid, idempotent (same `IdempotencyKey`), missing items
- Cancel wager — pending → cancelled, already-settled returns 409
- Get games with current lines
- Health endpoint

---

## Docker

```bash
# Build from modernization/ root
docker build -f betting-service/Dockerfile -t cog/betting-service:local .

# Run
docker run -p 5050:8080 \
  -e ConnectionStrings__DefaultConnection="..." \
  -e ConnectionStrings__Redis="..." \
  -e Jwt__SecretKey="..." \
  cog/betting-service:local
```

---

## Kubernetes

Manifests in `k8s/`: deployment (2 replicas), ClusterIP service, HPA (2–10 replicas).

---

## Known Issues

| Issue | Cause | Fix |
|---|---|---|
| `IDX10517 The signature key was not found` | .NET 10 JWT bearer defaults to `JsonWebTokenHandler`, which rejects tokens without a `kid` header | Fixed — `UseSecurityTokenValidators = true` in `AddJwtBearer` forces legacy `JwtSecurityTokenHandler` |
| `Cannot generate SSPI context` | `Trusted_Connection=True` fails on localhost SQL Server | Use SQL auth in `appsettings.Development.json`: `User ID=sa;Password=...` |

---

## Architecture Notes

- **Idempotency** — `POST /api/wagers` accepts an optional `IdempotencyKey` header. The same key within 24 hours returns the original response without a duplicate wager. Implemented via a filtered unique index on `Wagers.IdempotencyKey`.
- **Shared domain entities** — uses `Cog.Domain` (`shared/domain/`) for `Wager`, `Game`, `Agent`, `Customer` etc. so entity definitions are consistent across services.
- **Payout calculations** — standard -110 juice (risk $110, win $100), parlay multipliers, and teaser point adjustments are computed in `Services/PayoutCalculatorService.cs`, ported from the Delphi `BetMaker` business logic.
- **Real-time odds** — the `betting-ui` receives live odds updates via the `alerts-service` SignalR hub; `betting-service` publishes line change events to Redis.
- **Audit log** — every wager creation and cancellation is written to `AuditLogs` with before/after JSON snapshots.
- **EF Core migrations** — run `dotnet ef migrations add <Name>` from `src/` to create new migrations.
