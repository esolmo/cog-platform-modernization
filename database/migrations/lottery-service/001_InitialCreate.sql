-- =============================================================================
-- lottery-service: Initial Schema
-- Replaces: Lottery/LotteryBusiness + LotteryDAL (LINQ-to-SQL)
-- Run order: 001
-- =============================================================================
SET NOCOUNT ON;
GO

-- ─── Lottery Games ────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LotteryGames')
CREATE TABLE LotteryGames (
    Id          INT           NOT NULL IDENTITY PRIMARY KEY,
    GameType    INT           NOT NULL,   -- 1=Pick3, 2=Pick4
    Name        NVARCHAR(100) NOT NULL,
    IsActive    BIT           NOT NULL DEFAULT 1
);
GO

-- ─── Drawing Details ─────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DrawingDetails')
CREATE TABLE DrawingDetails (
    Id              BIGINT        NOT NULL IDENTITY PRIMARY KEY,
    LotteryGameId   INT           NOT NULL,
    Name            NVARCHAR(100) NOT NULL,
    DrawingDate     DATETIME2     NOT NULL,
    TimeZoneId      NVARCHAR(100) NOT NULL DEFAULT 'UTC',
    MinutesToDraw   INT           NOT NULL DEFAULT 5,
    IsActive        BIT           NOT NULL DEFAULT 1,
    CONSTRAINT FK_DrawingDetails_Game FOREIGN KEY (LotteryGameId) REFERENCES LotteryGames(Id)
);
GO

-- ─── Lottery Tickets ─────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LotteryTickets')
CREATE TABLE LotteryTickets (
    Id              BIGINT         NOT NULL IDENTITY PRIMARY KEY,
    DrawingDetailId BIGINT         NOT NULL,
    CustomerId      INT            NOT NULL,
    AgentId         INT            NOT NULL,
    DateToPlay      DATE           NOT NULL,
    EventDate       DATETIME2      NOT NULL,
    Total           DECIMAL(18,2)  NOT NULL DEFAULT 0,
    Description     NVARCHAR(500)  NULL,
    PurchasedAt     DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_LotteryTickets_Drawing FOREIGN KEY (DrawingDetailId) REFERENCES DrawingDetails(Id)
);
GO

-- ─── Lottery Pick Entries ────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LotteryPickEntries')
CREATE TABLE LotteryPickEntries (
    Id              BIGINT         NOT NULL IDENTITY PRIMARY KEY,
    TicketId        BIGINT         NOT NULL,
    Number1         INT            NOT NULL,
    Number2         INT            NOT NULL,
    Number3         INT            NOT NULL,
    Number4         INT            NOT NULL DEFAULT 0,  -- 0 for Pick3
    PickType        INT            NOT NULL,   -- 1=Straight, 2=Boxed
    LotteryGameType INT            NOT NULL,   -- 1=Pick3, 2=Pick4
    PlayCount       INT            NOT NULL DEFAULT 1,
    Amount          DECIMAL(18,2)  NOT NULL,
    Cost            DECIMAL(18,2)  NOT NULL,
    Prize           DECIMAL(18,2)  NOT NULL DEFAULT 0,
    CONSTRAINT FK_LotteryPickEntries_Ticket FOREIGN KEY (TicketId) REFERENCES LotteryTickets(Id) ON DELETE CASCADE
);
GO

-- ─── Indexes ──────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DrawingDetails_GameId_Date')
    CREATE INDEX IX_DrawingDetails_GameId_Date ON DrawingDetails(LotteryGameId, DrawingDate);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LotteryTickets_CustomerId_PurchasedAt')
    CREATE INDEX IX_LotteryTickets_CustomerId_PurchasedAt ON LotteryTickets(CustomerId, PurchasedAt DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LotteryTickets_DrawingDetailId')
    CREATE INDEX IX_LotteryTickets_DrawingDetailId ON LotteryTickets(DrawingDetailId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LotteryPickEntries_TicketId')
    CREATE INDEX IX_LotteryPickEntries_TicketId ON LotteryPickEntries(TicketId);
GO
