# COG Platform Modernization

Modernized architecture for the COG betting and casino management platform. Replaces a 2014-era stack (Delphi Win32 services, WCF, Classic ASP, Node.js) with cloud-native .NET 8 microservices, React TypeScript SPAs, and AWS infrastructure managed via Terraform.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                            AWS Cloud                                │
│                                                                     │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │                        EKS Cluster (cog)                       │ │
│  │                                                                │ │
│  │  auth-service    accounts-service   betting-service            │ │
│  │  :5001           :5002              :5005                      │ │
│  │                                                                │ │
│  │  admin-service   alerts-service     lottery-service            │ │
│  │  :5003           :5004 (+SignalR)   :5006                      │ │
│  │                                                                │ │
│  │  reports-service                                               │ │
│  │  :5007                                                         │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │              S3 + CloudFront (React SPAs)                      │ │
│  │                                                                │ │
│  │  betting-ui      accounts-ui        admin-ui                   │ │
│  │  lottery-ui      reports-ui                                    │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  RDS SQL Server  ·  ElastiCache Redis  ·  ALB  ·  API Gateway      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Services

| Service | Port | Description | Replaces |
|---|---|---|---|
| [auth-service](auth-service/) | 5001 | JWT auth, refresh tokens, user management | Delphi session + BitPermission bitmask |
| [accounts-service](accounts-service/) | 5002 | Customers, transactions, agent hierarchy | Accounts desktop + ASP accounts pages |
| [admin-service](admin-service/) | 5003 | Users, roles, system config, audit log | Admin + LoginsRoles desktops |
| [alerts-service](alerts-service/) | 5004 | Real-time alerts via SignalR + Redis | Node.js InstantAction (Socket.IO) |
| [betting-service](betting-service/) | 5005 | Wagers, games, lines, sports | BetMaker desktop + Cog-web-betting |
| [lottery-service](lottery-service/) | 5006 | Pick3/Pick4 lottery tickets | Lottery WebForms app |
| [reports-service](reports-service/) | 5007 | Wager/transaction/agent reports | WebReports WebForms app |

## Front-end Apps

| App | Dev Port | Description | Replaces |
|---|---|---|---|
| [betting-ui](betting-ui/) | 5173 | Sport → Game → Wager → Confirmation flow | Cog-web-betting ASP pages + BetMaker desktop |
| [accounts-ui](accounts-ui/) | 5174 | Customer account dashboard, transactions | Accounts desktop + ASP account pages |
| [admin-ui](admin-ui/) | 5175 | User/role management, system config | Admin + LoginsRoles desktops |
| [lottery-ui](lottery-ui/) | 5176 | Pick3/Pick4 ticket purchase | Lottery WebForms |
| [reports-ui](reports-ui/) | 5177 | Wager/transaction/agent reports | WebReports WebForms |

## Shared

| Package | Description |
|---|---|
| [shared/domain](shared/domain/) | `Cog.Domain` — C# entity classes shared across all services |
| [database](database/) | EF Core–compatible DDL scripts + seed data |
| [terraform](terraform/) | AWS infrastructure-as-code (EKS, RDS, Redis, ALB, S3/CloudFront) |

---

## Technology Stack

| Layer | Technology |
|---|---|
| Backend | .NET 8 LTS, ASP.NET Core Web API |
| Real-time | ASP.NET Core SignalR + Redis backplane |
| Frontend | React 18, TypeScript (strict), Vite |
| Auth | ASP.NET Core Identity, JWT (15 min) + refresh tokens in Redis (7 days) |
| ORM | Entity Framework Core 8 |
| Database | SQL Server (RDS on AWS, local SQL Server for dev) |
| Cache / PubSub | Redis (ElastiCache on AWS, local Redis for dev) |
| Logging | Serilog → CloudWatch Logs |
| Tracing | OpenTelemetry → AWS X-Ray |
| Container | Docker (multi-stage builds, non-root user) |
| Orchestration | Kubernetes on EKS via Terraform |
| CI/CD | GitHub Actions |
| Unit tests | xUnit (.NET), Vitest (TypeScript) |
| Integration tests | xUnit + Testcontainers (SQL Server + Redis) |
| E2E tests | Playwright |

---

## Prerequisites

### Backend
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- SQL Server (local instance or Docker: `docker run -e ACCEPT_EULA=Y -e SA_PASSWORD=Dev_P@ss1 -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest`)
- Redis (local or Docker: `docker run -p 6379:6379 redis:7-alpine`)
- Docker (for integration tests with Testcontainers)

### Frontend
- Node.js 20+
- npm 10+

### Infrastructure
- Terraform 1.7+
- AWS CLI (configured with appropriate credentials)
- `kubectl` (for EKS interaction)

---

## Quick Start — Local Development

### 1. Start dependencies
```bash
# SQL Server
docker run -d --name cog-sql \
  -e ACCEPT_EULA=Y -e SA_PASSWORD=Dev_P@ss1 \
  -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2022-latest

# Redis
docker run -d --name cog-redis \
  -p 6379:6379 \
  redis:7-alpine
```

### 2. Run database migrations
Each service manages its own schema slice. Run from each service directory:
```bash
cd auth-service/src
dotnet ef database update

cd ../../accounts-service/src
dotnet ef database update

# ... repeat for each service
```

Or apply the raw SQL scripts in order:
```bash
cd database
# See database/README.md for sqlcmd instructions
```

### 3. Start backend services
```bash
# Start all services (each in its own terminal or use a process manager)
cd auth-service/src     && dotnet run
cd accounts-service/src && dotnet run
cd admin-service/src    && dotnet run
cd alerts-service/src   && dotnet run
cd betting-service/src  && dotnet run
cd lottery-service/src  && dotnet run
cd reports-service/src  && dotnet run
```

### 4. Start frontend apps
```bash
cd betting-ui   && npm install && npm run dev   # http://localhost:5173
cd accounts-ui  && npm install && npm run dev   # http://localhost:5174
cd admin-ui     && npm install && npm run dev   # http://localhost:5175
cd lottery-ui   && npm install && npm run dev   # http://localhost:5176
cd reports-ui   && npm install && npm run dev   # http://localhost:5177
```

---

## Running All Tests

### Backend unit tests
```bash
# From repo root
find modernization -name "*.Tests.csproj" | while read p; do
  dotnet test "$p" --no-build -v minimal
done
```

### Backend integration tests (requires Docker)
```bash
# Testcontainers spins up SQL Server + Redis automatically
dotnet test auth-service/tests
dotnet test betting-service/tests
dotnet test accounts-service/tests
# ... etc.
```

### Frontend unit tests
```bash
cd betting-ui  && npm test
cd accounts-ui && npm test
# ... etc.
```

### E2E tests
```bash
# Ensure the target service is running first
cd betting-ui  && npm run test:e2e
cd accounts-ui && npm run test:e2e
```

---

## CI/CD

| Workflow | File | Trigger |
|---|---|---|
| .NET services build + test | `.github/workflows/dotnet-services.yml` | Push / PR |
| Frontend apps build + test | `.github/workflows/frontend-apps.yml` | Push / PR |
| Terraform validate + plan | `.github/workflows/terraform.yml` | Push / PR |
| Terraform apply | `.github/workflows/terraform.yml` | Merge to `main` |

---

## Deployment

See [terraform/README.md](terraform/README.md) for full deployment instructions.

```bash
# Deploy to staging
cd terraform/environments/staging
terraform init
terraform plan -out=tfplan
terraform apply tfplan

# Deploy to prod
cd ../prod
terraform plan -out=tfplan
terraform apply tfplan
```

Docker images are built and pushed to ECR by GitHub Actions. The EKS deployment uses image tags from the CI pipeline.

---

## Project Structure

```
modernization/
├── auth-service/           # JWT auth (.NET 8)
├── betting-service/        # Wagers, games, lines (.NET 8)
├── betting-ui/             # Web betting SPA (React/TS)
├── accounts-service/       # Customers, transactions (.NET 8)
├── accounts-ui/            # Account management SPA (React/TS)
├── admin-service/          # Users, roles, config (.NET 8)
├── admin-ui/               # Admin SPA (React/TS)
├── alerts-service/         # SignalR real-time alerts (.NET 8)
├── lottery-service/        # Pick3/Pick4 lottery (.NET 8)
├── lottery-ui/             # Lottery SPA (React/TS)
├── reports-service/        # Reporting API (.NET 8)
├── reports-ui/             # Reports dashboard SPA (React/TS)
├── shared/
│   └── domain/             # Cog.Domain shared entity library
├── database/
│   ├── migrations/         # Per-service DDL scripts
│   └── seed/               # Reference data scripts
└── terraform/
    ├── modules/            # Reusable Terraform modules
    └── environments/       # dev / staging / prod configs
```

Each .NET service follows the same layout:
```
{service}/
├── src/
│   ├── Controllers/        # ASP.NET Core controllers
│   ├── Services/           # Business logic
│   ├── Data/               # DbContext + repositories
│   ├── Entities/           # EF Core entity models
│   ├── Models/             # Request/response DTOs
│   ├── Migrations/         # EF Core migrations
│   └── Program.cs          # Host setup + DI
├── tests/
│   ├── Unit/               # xUnit unit tests
│   └── Integration/        # Testcontainers integration tests
├── Dockerfile
├── k8s/                    # Kubernetes manifests
└── {Service}.sln
```

Each React app follows the same layout:
```
{app}/
├── src/
│   ├── api/                # Axios API client functions
│   ├── components/         # Shared UI components
│   ├── pages/              # Route-level page components
│   ├── stores/             # Zustand state stores
│   ├── types/              # TypeScript type definitions
│   └── test/               # Vitest test setup
├── e2e/                    # Playwright E2E specs
├── playwright.config.ts
├── vite.config.ts
└── package.json
```

---

## Security Notes

- JWT secret keys in `appsettings.json` are placeholders — **replace with AWS Secrets Manager values before deploying**.
- The `appsettings.Development.json` files use relaxed settings (e.g., `TrustServerCertificate=True`) for local dev only.
- All Docker images run as a non-root user (`appuser`).
- CORS origins are explicitly configured per service — do not use wildcard (`*`) in production.

### JWT Secret Key Setup

All services share a single JWT secret — auth-service **signs** tokens, every other service **validates** them. They must all use the same key.

**Generate a key (run once, share across all services):**
```powershell
[Convert]::ToBase64String((1..32 | ForEach-Object { [byte](Get-Random -Max 256) }))
```

**Configure for local dev** — add to each service's `appsettings.Development.json`:
```json
{
  "Jwt": {
    "SecretKey": "6UZhMYYN1EqSmi7rtNvpRBS46WsNbVuYZwXzNft7h8c="
  }
}
```
The value above is the shared dev key already committed to `appsettings.Development.json` in each service. **Generate a new one for production.**

**Production** — store in AWS Secrets Manager as `cog/jwt-secret-key` and inject as `Jwt__SecretKey` env var in each service's Kubernetes deployment. The Terraform modules provision this automatically.

> SQL auth (`User Id=sa;Password=...`) is required for local dev — Windows Authentication (`Trusted_Connection=True`) fails on localhost with SSPI errors.
