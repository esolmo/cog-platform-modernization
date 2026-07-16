-- =============================================================================
-- accounts-service: Initial Schema
-- Replaces: Accounts/ Delphi desktop + Cog-web-betting/Accounts/ ASP pages
-- Source analysis: COGLib/Agent/Agent.cs, Sps/dbCreate/InsertNewCustomer.sql,
--                  Sps/dbCreate/UpdateCustomerCreditLimit.sql
-- Run order: 001
-- =============================================================================
SET NOCOUNT ON;
GO

-- ─── Agents ───────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Agents')
CREATE TABLE Agents (
    Id                  INT           NOT NULL IDENTITY PRIMARY KEY,
    ParentAgentId       INT           NULL,
    LoginName           NVARCHAR(50)  NOT NULL,
    FullName            NVARCHAR(200) NULL,
    Email               NVARCHAR(200) NULL,
    Phone               NVARCHAR(50)  NULL,
    -- Legacy fields preserved for migration mapping
    LegacyAgentId       INT           NULL,
    MaxAccessLevel      INT           NOT NULL DEFAULT 0,
    IsActive            BIT           NOT NULL DEFAULT 1,
    CreatedAt           DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt           DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Agents_Parent FOREIGN KEY (ParentAgentId) REFERENCES Agents(Id)
);
GO

-- ─── Customers ────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Customers')
CREATE TABLE Customers (
    Id                  INT            NOT NULL IDENTITY PRIMARY KEY,
    AgentId             INT            NOT NULL,
    LoginName           NVARCHAR(50)   NOT NULL,
    PasswordHash        NVARCHAR(256)  NOT NULL,
    FirstName           NVARCHAR(100)  NULL,
    LastName            NVARCHAR(100)  NULL,
    Email               NVARCHAR(200)  NULL,
    Phone               NVARCHAR(50)   NULL,
    IsActive            BIT            NOT NULL DEFAULT 1,
    -- Legacy field for migration mapping
    LegacyCustomerId    INT            NULL,
    CreatedAt           DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt           DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Customers_Agent FOREIGN KEY (AgentId) REFERENCES Agents(Id)
);
GO

-- ─── Customer Limits ──────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CustomerLimits')
CREATE TABLE CustomerLimits (
    Id                  INT            NOT NULL IDENTITY PRIMARY KEY,
    CustomerId          INT            NOT NULL UNIQUE,
    CreditLimit         DECIMAL(18,2)  NOT NULL DEFAULT 0,
    BalanceType         TINYINT        NOT NULL DEFAULT 0,  -- 0=Credit, 1=Debit
    MaxWager            DECIMAL(18,2)  NULL,
    MaxPayout           DECIMAL(18,2)  NULL,
    -- Legacy bitmask flags preserved for migration phase
    BitLimits           BIGINT         NOT NULL DEFAULT 0,
    UpdatedAt           DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_CustomerLimits_Customer FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE CASCADE
);
GO

-- ─── Transactions ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Transactions')
CREATE TABLE Transactions (
    Id                  BIGINT         NOT NULL IDENTITY PRIMARY KEY,
    CustomerId          INT            NOT NULL,
    DocumentNumber      NVARCHAR(50)   NULL,
    TranType            NVARCHAR(50)   NOT NULL,   -- Deposit, Withdrawal, Wager, Payout, Adjustment
    Amount              DECIMAL(18,2)  NOT NULL,
    RunningBalance      DECIMAL(18,2)  NOT NULL DEFAULT 0,
    Description         NVARCHAR(500)  NULL,
    Reference           NVARCHAR(200)  NULL,
    CreatedAt           DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy           NVARCHAR(100)  NULL,
    -- Soft update tracking (mirrors UpdatedCustomerTransaction)
    UpdatedAt           DATETIME2      NULL,
    UpdatedBy           NVARCHAR(100)  NULL,
    UpdatedReason       NVARCHAR(500)  NULL,
    CONSTRAINT FK_Transactions_Customer FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
);
GO

-- ─── Free Plays ───────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'FreePlays')
CREATE TABLE FreePlays (
    Id                  INT            NOT NULL IDENTITY PRIMARY KEY,
    CustomerId          INT            NOT NULL,
    Amount              DECIMAL(18,2)  NOT NULL,
    ExpiresAt           DATETIME2      NULL,
    IsUsed              BIT            NOT NULL DEFAULT 0,
    UsedAt              DATETIME2      NULL,
    CreatedAt           DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy           NVARCHAR(100)  NULL,
    CONSTRAINT FK_FreePlays_Customer FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE CASCADE
);
GO

-- ─── Indexes ──────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Agents_LoginName')
    CREATE UNIQUE INDEX IX_Agents_LoginName ON Agents(LoginName);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Agents_ParentAgentId')
    CREATE INDEX IX_Agents_ParentAgentId ON Agents(ParentAgentId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Customers_LoginName')
    CREATE UNIQUE INDEX IX_Customers_LoginName ON Customers(LoginName);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Customers_AgentId')
    CREATE INDEX IX_Customers_AgentId ON Customers(AgentId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Transactions_CustomerId_CreatedAt')
    CREATE INDEX IX_Transactions_CustomerId_CreatedAt ON Transactions(CustomerId, CreatedAt DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Transactions_TranType')
    CREATE INDEX IX_Transactions_TranType ON Transactions(TranType);
GO
