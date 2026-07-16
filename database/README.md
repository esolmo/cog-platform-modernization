# Database Migrations & Seed Data

## Structure

```
database/
├── migrations/
│   ├── auth-service/
│   │   └── 001_InitialCreate.sql   — Identity tables + RefreshTokens
│   ├── accounts-service/
│   │   └── 001_InitialCreate.sql   — Agents, Customers, Limits, Transactions, FreePlays
│   ├── alerts-service/
│   │   └── 001_InitialCreate.sql   — AlertTickets, AlertAttributes, AgentVipSettings
│   ├── admin-service/
│   │   └── 001_InitialCreate.sql   — ApplicationUsers, Roles, Permissions, AuditLogs
│   ├── lottery-service/
│   │   └── 001_InitialCreate.sql   — LotteryGames, DrawingDetails, Tickets, Picks
│   └── betting-service/
│       └── 001_InitialCreate.sql   — SportTypes, Agents, Games, Customers, Wagers, Lines
└── seed/
    ├── 01_sport_types.sql           — Legacy sport type codes (FB, BB, BL, etc.) — shared/legacy use
    ├── 02_admin_permissions.sql     — 17 named permissions (replaces BitPermission bitmask)
    ├── 03_admin_roles.sql           — System roles + operational roles + permission assignments
    ├── 04_lottery_games.sql         — Pick 3 / Pick 4 games + 7-day rolling drawings
    ├── 05_system_config_defaults.sql — Platform configuration defaults
    └── 06_betting_sport_types.sql   — Sport types for COGDB_Betting (NFL, NBA, MLB, etc.)
```

## Running Migrations

Each migration script is **idempotent** — safe to re-run. Scripts check for table/index existence before creating.

### Development (run in order per service)

```sql
-- 1. Create databases (once)
CREATE DATABASE CogAuth;
CREATE DATABASE CogAccounts;
CREATE DATABASE CogAlerts;
CREATE DATABASE CogAdmin;
CREATE DATABASE CogLottery;
CREATE DATABASE COGDB_Betting;

-- 2. Run per-service migrations
USE CogAuth;       EXEC sp_executesql N'...'; -- or run file via sqlcmd
USE CogAccounts;   -- etc.

-- 3. Run seed data (against CogAdmin and CogLottery)
USE CogAdmin;
-- run 02_admin_permissions.sql
-- run 03_admin_roles.sql

USE CogLottery;
-- run 04_lottery_games.sql

-- run 05_system_config_defaults.sql against CogAdmin
```

### Via sqlcmd (recommended for CI)

```bash
# Auth service schema
sqlcmd -S $DB_HOST -U $DB_USER -P $DB_PASS -d CogAuth \
  -i migrations/auth-service/001_InitialCreate.sql

# Accounts service schema
sqlcmd -S $DB_HOST -U $DB_USER -P $DB_PASS -d CogAccounts \
  -i migrations/accounts-service/001_InitialCreate.sql

# etc...

# Seed data
sqlcmd -S $DB_HOST -U $DB_USER -P $DB_PASS -d CogAdmin \
  -i seed/02_admin_permissions.sql
sqlcmd -S $DB_HOST -U $DB_USER -P $DB_PASS -d CogAdmin \
  -i seed/03_admin_roles.sql
sqlcmd -S $DB_HOST -U $DB_USER -P $DB_PASS -d CogLottery \
  -i seed/04_lottery_games.sql
sqlcmd -S $DB_HOST -U $DB_USER -P $DB_PASS -d CogAdmin \
  -i seed/05_system_config_defaults.sql

# Betting service schema + sport type seed
sqlcmd -S $DB_HOST -U $DB_USER -P $DB_PASS -d COGDB_Betting \
  -i migrations/betting-service/001_InitialCreate.sql
sqlcmd -S $DB_HOST -U $DB_USER -P $DB_PASS \
  -i seed/06_betting_sport_types.sql   # uses USE COGDB_Betting internally
```

## EF Core Migrations (in-app)

Each service also drives its schema via EF Core. The SQL files above are the explicit DDL equivalents — useful for:
- Schema reviews before deployment
- Running against existing databases where EF migrations cannot be applied directly
- Auditing what each service owns

To generate fresh EF Core migrations:

```bash
cd modernization/accounts-service/src
dotnet ef migrations add InitialCreate --output-dir Migrations

cd modernization/admin-service/src
dotnet ef migrations add InitialCreate --output-dir Migrations
# etc.
```

## Naming Conventions

- Migration files: `NNN_PascalCaseName.sql` (three-digit prefix for ordering)
- Seed files: `NN_snake_case_description.sql`
- All scripts are T-SQL (SQL Server syntax) for AWS RDS SQL Server SE 15.x
