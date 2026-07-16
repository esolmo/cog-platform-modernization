# Cog.Domain

Shared C# entity library for the COG platform. Contains domain entity classes and common value types shared across all .NET microservices. Published as an internal NuGet package (or referenced directly via `ProjectReference` in local development).

---

## Package

**Assembly:** `Cog.Domain`  
**Namespace root:** `Cog.Domain.Entities`  
**Target framework:** .NET 8

---

## Entities

| Entity | Description |
|---|---|
| `Agent` | Betting agent; self-referencing hierarchy via `ParentAgentId` |
| `AgentPermission` | Per-agent feature flag overrides |
| `Customer` | Bettor account; belongs to an `Agent` |
| `CustomerBalance` | Current balance, credit limit, free-play balance (1-to-1 with Customer) |
| `CustomerLimits` | Wager limits (straight, parlay, teaser, if-bet) (1-to-1 with Customer) |
| `Wager` | A placed bet (straight, parlay, teaser, if-bet, reverse) |
| `WagerItem` | Individual leg within a wager (game, period, line, side, odds) |
| `Transaction` | Financial debit or credit against a customer account |
| `Game` | A scheduled sporting event |
| `GamePeriod` | A scoreable period within a game (full game, first half, etc.) |
| `LineSet` | Current spread/total/moneyline values for a game period |
| `LineShade` | Per-customer line adjustment override |
| `SportType` | Reference data: NFL, NBA, MLB, etc. |
| `AuditLog` | Immutable record of any entity mutation |
| `CasinoProfile` | Casino account for a customer |
| `CasinoTransaction` | Casino credit/debit transaction |

---

## Usage

### In a service's `.csproj`
```xml
<ItemGroup>
  <ProjectReference Include="..\..\shared\domain\Cog.Domain.csproj" />
</ItemGroup>
```

### In Docker builds
The `Dockerfile` for each service copies `shared/domain/` into the build context:
```dockerfile
COPY shared/domain/Cog.Domain.csproj shared/domain/
COPY auth-service/src/AuthService.csproj auth-service/src/
RUN dotnet restore auth-service/src/AuthService.csproj
COPY shared/domain/ shared/domain/
```

All service Dockerfiles must be built from the `modernization/` root directory, not from within the service directory.

---

## Adding Entities

1. Add the entity class to `Entities/`.
2. Add a `DbSet<T>` property to every `DbContext` that needs the entity.
3. Run `dotnet ef migrations add <Name>` from the relevant service's `src/` directory.
4. Update seed data scripts in `database/seed/` if reference data is needed.

---

## Design Conventions

- **No data annotations** — entity configuration uses EF Core Fluent API in each service's `DbContext.OnModelCreating`. This keeps domain entities free of infrastructure concerns.
- **No EF Core dependency** — `Cog.Domain.csproj` does not reference `Microsoft.EntityFrameworkCore`. Entities are plain C# classes.
- **Nullable reference types** — all entities use NRT (`<Nullable>enable</Nullable>`). Required navigation properties are non-nullable; optional ones are `?`.
- **Value objects** — common value types (e.g., money amounts) should eventually be extracted to value objects here to enforce invariants across services.
