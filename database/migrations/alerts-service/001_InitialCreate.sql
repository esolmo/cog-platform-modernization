-- =============================================================================
-- alerts-service: Initial Schema
-- Replaces: InstantAction/ac/app.js (Node.js Socket.IO) + COGLib/ActionAlert/
-- Run order: 001
-- =============================================================================
SET NOCOUNT ON;
GO

-- ─── Alert Tickets ────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AlertTickets')
CREATE TABLE AlertTickets (
    Id              BIGINT         NOT NULL IDENTITY PRIMARY KEY,
    WagerNumber     NVARCHAR(100)  NOT NULL,
    CustomerId      INT            NOT NULL,
    AgentId         INT            NOT NULL,
    AlertType       INT            NOT NULL,   -- 1=Wager,2=Parlay,3=Teaser,4=Casino,6=Horse,7=Lottery,13=Contest
    WagerType       INT            NOT NULL,
    Amount          DECIMAL(18,2)  NOT NULL,
    IsSharp         BIT            NOT NULL DEFAULT 0,
    IsSquare        BIT            NOT NULL DEFAULT 0,
    ExpiresAt       DATETIME2      NULL,
    IsUnalerted     BIT            NOT NULL DEFAULT 0,
    UnalertedAt     DATETIME2      NULL,
    CreatedAt       DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

-- ─── Alert Attributes ─────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AlertAttributes')
CREATE TABLE AlertAttributes (
    Id          INT            NOT NULL IDENTITY PRIMARY KEY,
    TicketId    BIGINT         NOT NULL,
    Name        NVARCHAR(100)  NOT NULL,
    Value       NVARCHAR(500)  NULL,
    CONSTRAINT FK_AlertAttributes_Ticket FOREIGN KEY (TicketId) REFERENCES AlertTickets(Id) ON DELETE CASCADE
);
GO

-- ─── Alert Details ────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AlertDetails')
CREATE TABLE AlertDetails (
    Id          INT            NOT NULL IDENTITY PRIMARY KEY,
    TicketId    BIGINT         NOT NULL,
    SportCode   NVARCHAR(10)   NULL,
    GameId      INT            NULL,
    Description NVARCHAR(500)  NULL,
    Line        NVARCHAR(100)  NULL,
    Odds        DECIMAL(10,4)  NULL,
    CONSTRAINT FK_AlertDetails_Ticket FOREIGN KEY (TicketId) REFERENCES AlertTickets(Id) ON DELETE CASCADE
);
GO

-- ─── Alert Detail Attributes ──────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AlertDetailAttributes')
CREATE TABLE AlertDetailAttributes (
    Id          INT            NOT NULL IDENTITY PRIMARY KEY,
    DetailId    INT            NOT NULL,
    Name        NVARCHAR(100)  NOT NULL,
    Value       NVARCHAR(500)  NULL,
    CONSTRAINT FK_AlertDetailAttributes_Detail FOREIGN KEY (DetailId) REFERENCES AlertDetails(Id) ON DELETE CASCADE
);
GO

-- ─── Agent VIP Settings ───────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AgentVipSettings')
CREATE TABLE AgentVipSettings (
    Id                  INT            NOT NULL IDENTITY PRIMARY KEY,
    AgentId             INT            NOT NULL UNIQUE,
    NotificationEmail   NVARCHAR(200)  NULL,
    MinimumAlertAmount  DECIMAL(18,2)  NOT NULL DEFAULT 0,
    IsEnabled           BIT            NOT NULL DEFAULT 1,
    CreatedAt           DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt           DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

-- ─── Indexes ──────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AlertTickets_AgentId_CreatedAt')
    CREATE INDEX IX_AlertTickets_AgentId_CreatedAt ON AlertTickets(AgentId, CreatedAt DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AlertTickets_AlertType')
    CREATE INDEX IX_AlertTickets_AlertType ON AlertTickets(AlertType);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AlertTickets_ExpiresAt')
    CREATE INDEX IX_AlertTickets_ExpiresAt ON AlertTickets(ExpiresAt) WHERE ExpiresAt IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AlertTickets_WagerNumber')
    CREATE INDEX IX_AlertTickets_WagerNumber ON AlertTickets(WagerNumber);
GO
