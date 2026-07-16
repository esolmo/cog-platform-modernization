# alerts-service

Real-time alert notification service for the COG platform. Broadcasts betting alerts to connected agents via SignalR, with Redis as the pub/sub backplane for horizontal scaling. Replaces the Node.js `InstantAction` service (Socket.IO + Redis).

**Port:** `5040` (http) / `5041` (https)  
**Database:** `COGDB` (SQL Server, shared schema slice)  
**Cache / PubSub:** Redis (SignalR backplane + alert expiry)

---

## API Endpoints

### Alerts

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/api/alerts` | Bearer | Get active alerts for an agent (`?agentId=N`) |
| `POST` | `/api/alerts` | Bearer | Create a new alert |
| `DELETE` | `/api/alerts/{id}` | Bearer | Dismiss (delete) an alert |

### VIP Settings

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/api/alerts/vip-settings/{agentId}` | Bearer | Get VIP customer alert thresholds for an agent |
| `PUT` | `/api/alerts/vip-settings/{agentId}` | Bearer | Update VIP alert settings |

### Health

| Method | Route | Description |
|---|---|---|
| `GET` | `/health/live` | Liveness probe |
| `GET` | `/health/ready` | Readiness probe (checks DB + Redis) |

---

## SignalR Hub

**Hub URL:** `/hubs/alerts`

### Server → Client events

| Event | Payload | Description |
|---|---|---|
| `AlertCreated` | `AlertSummary` | New alert for this agent |
| `AlertDismissed` | `{ id: number }` | Alert was dismissed |
| `OddsUpdated` | `{ gameId, periodId, lineType, value }` | Live line change |

### Client → Server events

| Event | Args | Description |
|---|---|---|
| `JoinAgentGroup` | `agentId: number` | Subscribe to alerts for a specific agent |
| `LeaveAgentGroup` | `agentId: number` | Unsubscribe |

### JavaScript / TypeScript client example
```typescript
import * as signalR from '@microsoft/signalr'

const connection = new signalR.HubConnectionBuilder()
  .withUrl('/hubs/alerts', {
    accessTokenFactory: () => localStorage.getItem('accessToken') ?? ''
  })
  .withAutomaticReconnect()
  .build()

connection.on('AlertCreated', (alert) => {
  console.log('New alert', alert)
})

await connection.start()
await connection.invoke('JoinAgentGroup', agentId)
```

---

## Configuration

`src/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=COGDB;...",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "SecretKey": "CHANGE_ME_IN_PRODUCTION_USE_A_LONG_RANDOM_SECRET",
    "Issuer": "cog-auth-service",
    "Audience": "cog-services"
  },
  "Email": {
    "From": "alerts@cog.local",
    "Host": "smtp.example.com",
    "Port": "587",
    "Username": "",
    "Password": ""
  },
  "AllowedOrigins": [
    "http://localhost:5173",
    "http://localhost:5174"
  ]
}
```

| Variable | Description |
|---|---|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `ConnectionStrings__Redis` | Redis connection string (also used as SignalR backplane) |
| `Jwt__SecretKey` | Same key as auth-service (token validation only) |
| `Email__Host` / `Email__Password` | SMTP for email alert delivery (store password in AWS Secrets Manager) |
| `AllowedOrigins` | CORS whitelist for SignalR WebSocket upgrade |

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
# Requires Redis running
docker run -d -p 6379:6379 redis:7-alpine

cd src
dotnet ef database update
dotnet run --launch-profile http
# Listening on http://localhost:5040
```

### Ports

| Profile | URL |
|---|---|
| `http` | `http://localhost:5040` |
| `https` | `https://localhost:5041` |

### Swagger UI

Open `http://localhost:5040/swagger`.

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

Testcontainers starts SQL Server 2022 + Redis 7 Alpine containers.

Key coverage:
- `GET /api/alerts` — returns 200, agent-scoped filtering (no cross-agent leakage)
- `POST /api/alerts` — valid request returns 201
- `DELETE /api/alerts/{id}` — 204 on success, 404 for unknown ID
- `GET /api/alerts/vip-settings/{agentId}` — returns 200
- SignalR hub connection test — asserts `HubConnectionState.Connected`

---

## Docker

```bash
docker build -f alerts-service/Dockerfile -t cog/alerts-service:local .

docker run -p 5040:8080 \
  -e ConnectionStrings__DefaultConnection="..." \
  -e ConnectionStrings__Redis="..." \
  -e Jwt__SecretKey="..." \
  cog/alerts-service:local
```

---

## Kubernetes

Manifests in `k8s/`. The Redis backplane requires a shared Redis instance across all replicas — provided by ElastiCache in production.

---

## Known Issues

| Issue | Cause | Fix |
|---|---|---|
| `IDX10517 The signature key was not found` | .NET 10 JWT bearer defaults to `JsonWebTokenHandler`, which rejects tokens without a `kid` header | Fixed — `UseSecurityTokenValidators = true` in `AddJwtBearer` forces legacy `JwtSecurityTokenHandler` |
| `Cannot generate SSPI context` | `Trusted_Connection=True` fails on localhost SQL Server | Use SQL auth in `appsettings.Development.json`: `User ID=sa;Password=...` |

---

## Architecture Notes

- **Redis backplane** — `AddStackExchangeRedis()` on the SignalR builder enables pub/sub across multiple service instances. All pods in the EKS deployment share one ElastiCache Redis cluster.
- **Agent groups** — each connected agent joins a SignalR group named `agent-{id}`. When a wager alert is created, the hub sends only to the relevant agent's group. Sub-agent hierarchy alerts broadcast to all ancestor agent groups.
- **Alert expiry** — `AlertTicket.ExpiresAt` is set at creation time. A background `IHostedService` runs every minute to purge expired alerts from the DB and remove them from connected clients via the hub.
- **Email notifications** — optional email delivery via MailKit for agents who have email alerts enabled in their VIP settings.
- **CORS** — only origins in `AllowedOrigins` may establish a WebSocket connection. In production this should be the CloudFront distribution domain(s) only.
