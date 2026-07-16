-- ============================================================
-- betting-service : Initial Schema
-- Database : COGDB_Betting
-- Mirrors   : BettingService/src/Migrations/20260409120000_InitialCreate.cs
-- Idempotent: all CREATE statements are guarded by IF NOT EXISTS
-- Run order : execute after the database itself has been created
-- ============================================================

SET NOCOUNT ON;
GO

-- ── AuditLogs ────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AuditLogs')
BEGIN
    CREATE TABLE [dbo].[AuditLogs] (
        [Id]          bigint          NOT NULL IDENTITY(1,1),
        [EntityType]  nvarchar(100)   NOT NULL,
        [EntityId]    int             NOT NULL,
        [Action]      nvarchar(100)   NOT NULL,
        [OldValues]   nvarchar(max)   NULL,
        [NewValues]   nvarchar(max)   NULL,
        [PerformedBy] nvarchar(100)   NOT NULL,
        [IpAddress]   nvarchar(50)    NULL,
        [PerformedAt] datetime2       NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE INDEX [IX_AuditLogs_EntityType_EntityId] ON [dbo].[AuditLogs] ([EntityType], [EntityId]);
    CREATE INDEX [IX_AuditLogs_PerformedAt]         ON [dbo].[AuditLogs] ([PerformedAt]);

    PRINT 'Created table AuditLogs';
END
GO

-- ── SportTypes ───────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SportTypes')
BEGIN
    CREATE TABLE [dbo].[SportTypes] (
        [Id]           int           NOT NULL IDENTITY(1,1),
        [Name]         nvarchar(100) NOT NULL,
        [Code]         nvarchar(20)  NOT NULL,
        [IsActive]     bit           NOT NULL CONSTRAINT [DF_SportTypes_IsActive] DEFAULT (1),
        [DisplayOrder] int           NOT NULL,
        CONSTRAINT [PK_SportTypes] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE UNIQUE INDEX [IX_SportTypes_Code] ON [dbo].[SportTypes] ([Code]);

    PRINT 'Created table SportTypes';
END
GO

-- ── Agents ───────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Agents')
BEGIN
    CREATE TABLE [dbo].[Agents] (
        [Id]           int           NOT NULL IDENTITY(1,1),
        [LoginName]    nvarchar(50)  NOT NULL,
        [PasswordHash] nvarchar(256) NOT NULL,
        [Name]         nvarchar(200) NOT NULL,
        [Email]        nvarchar(200) NULL,
        [ParentAgentId] int          NULL,
        [AgentType]    int           NOT NULL,
        [IsActive]     bit           NOT NULL CONSTRAINT [DF_Agents_IsActive] DEFAULT (1),
        [CreatedAt]    datetime2     NOT NULL,
        [UpdatedAt]    datetime2     NULL,
        CONSTRAINT [PK_Agents] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Agents_Agents_ParentAgentId]
            FOREIGN KEY ([ParentAgentId]) REFERENCES [dbo].[Agents] ([Id])
            ON DELETE NO ACTION
    );

    CREATE UNIQUE INDEX [IX_Agents_LoginName]    ON [dbo].[Agents] ([LoginName]);
    CREATE        INDEX [IX_Agents_ParentAgentId] ON [dbo].[Agents] ([ParentAgentId]);

    PRINT 'Created table Agents';
END
GO

-- ── Games ────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Games')
BEGIN
    CREATE TABLE [dbo].[Games] (
        [Id]             int           NOT NULL IDENTITY(1,1),
        [SportTypeId]    int           NOT NULL,
        [HomeTeam]       nvarchar(100) NOT NULL,
        [AwayTeam]       nvarchar(100) NOT NULL,
        [GameDate]       datetime2     NOT NULL,
        [Status]         int           NOT NULL,
        [RotationNumber] nvarchar(20)  NULL,
        [CreatedAt]      datetime2     NOT NULL,
        [UpdatedAt]      datetime2     NULL,
        CONSTRAINT [PK_Games] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Games_SportTypes_SportTypeId]
            FOREIGN KEY ([SportTypeId]) REFERENCES [dbo].[SportTypes] ([Id])
            ON DELETE NO ACTION
    );

    CREATE INDEX [IX_Games_SportTypeId] ON [dbo].[Games] ([SportTypeId]);
    CREATE INDEX [IX_Games_GameDate]    ON [dbo].[Games] ([GameDate]);

    PRINT 'Created table Games';
END
GO

-- ── AgentPermissions ─────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AgentPermissions')
BEGIN
    CREATE TABLE [dbo].[AgentPermissions] (
        [Id]            int           NOT NULL IDENTITY(1,1),
        [AgentId]       int           NOT NULL,
        [PermissionKey] nvarchar(100) NOT NULL,
        [IsGranted]     bit           NOT NULL CONSTRAINT [DF_AgentPermissions_IsGranted] DEFAULT (1),
        CONSTRAINT [PK_AgentPermissions] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_AgentPermissions_Agents_AgentId]
            FOREIGN KEY ([AgentId]) REFERENCES [dbo].[Agents] ([Id])
            ON DELETE CASCADE
    );

    CREATE INDEX [IX_AgentPermissions_AgentId] ON [dbo].[AgentPermissions] ([AgentId]);

    PRINT 'Created table AgentPermissions';
END
GO

-- ── Customers ────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Customers')
BEGIN
    CREATE TABLE [dbo].[Customers] (
        [Id]           int            NOT NULL IDENTITY(1,1),
        [LoginName]    nvarchar(50)   NOT NULL,
        [PasswordHash] nvarchar(256)  NOT NULL,
        [FirstName]    nvarchar(100)  NOT NULL,
        [LastName]     nvarchar(100)  NOT NULL,
        [Email]        nvarchar(200)  NULL,
        [Phone]        nvarchar(50)   NULL,
        [AgentId]      int            NOT NULL,
        [Status]       int            NOT NULL,
        [IsVip]        bit            NOT NULL,
        [CreatedAt]    datetime2      NOT NULL,
        [UpdatedAt]    datetime2      NULL,
        [Notes]        nvarchar(1000) NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Customers_Agents_AgentId]
            FOREIGN KEY ([AgentId]) REFERENCES [dbo].[Agents] ([Id])
            ON DELETE NO ACTION
    );

    CREATE UNIQUE INDEX [IX_Customers_LoginName] ON [dbo].[Customers] ([LoginName]);
    CREATE        INDEX [IX_Customers_AgentId]   ON [dbo].[Customers] ([AgentId]);

    PRINT 'Created table Customers';
END
GO

-- ── GamePeriods ──────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'GamePeriods')
BEGIN
    CREATE TABLE [dbo].[GamePeriods] (
        [Id]                int           NOT NULL IDENTITY(1,1),
        [GameId]            int           NOT NULL,
        [PeriodDescription] nvarchar(100) NOT NULL,
        [PeriodNumber]      int           NOT NULL,
        CONSTRAINT [PK_GamePeriods] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_GamePeriods_Games_GameId]
            FOREIGN KEY ([GameId]) REFERENCES [dbo].[Games] ([Id])
            ON DELETE CASCADE
    );

    CREATE INDEX [IX_GamePeriods_GameId] ON [dbo].[GamePeriods] ([GameId]);

    PRINT 'Created table GamePeriods';
END
GO

-- ── CustomerBalances ─────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CustomerBalances')
BEGIN
    CREATE TABLE [dbo].[CustomerBalances] (
        [Id]                   int            NOT NULL IDENTITY(1,1),
        [CustomerId]           int            NOT NULL,
        [Balance]              decimal(18,2)  NOT NULL,
        [CreditLimit]          decimal(18,2)  NOT NULL,
        [TemporaryCreditLimit] decimal(18,2)  NOT NULL,
        [CasinoBalance]        decimal(18,2)  NOT NULL,
        [FreePlaysBalance]     decimal(18,2)  NOT NULL,
        [LastUpdated]          datetime2      NOT NULL,
        CONSTRAINT [PK_CustomerBalances] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_CustomerBalances_Customers_CustomerId]
            FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id])
            ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX [IX_CustomerBalances_CustomerId] ON [dbo].[CustomerBalances] ([CustomerId]);

    PRINT 'Created table CustomerBalances';
END
GO

-- ── CustomerLimits ───────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CustomerLimits')
BEGIN
    CREATE TABLE [dbo].[CustomerLimits] (
        [Id]               int           NOT NULL IDENTITY(1,1),
        [CustomerId]       int           NOT NULL,
        [MaxWagerStraight] decimal(18,2) NOT NULL,
        [MaxWagerParlay]   decimal(18,2) NOT NULL,
        [MaxWagerTeaser]   decimal(18,2) NOT NULL,
        [MaxWagerIfBet]    decimal(18,2) NOT NULL,
        [MaxWagerReverse]  decimal(18,2) NOT NULL,
        [MinWager]         decimal(18,2) NOT NULL,
        [MaxWinPerTicket]  decimal(18,2) NOT NULL,
        [AllowStraight]    bit           NOT NULL CONSTRAINT [DF_CustomerLimits_AllowStraight] DEFAULT (1),
        [AllowParlay]      bit           NOT NULL CONSTRAINT [DF_CustomerLimits_AllowParlay]   DEFAULT (1),
        [AllowTeaser]      bit           NOT NULL CONSTRAINT [DF_CustomerLimits_AllowTeaser]   DEFAULT (1),
        [AllowIfBet]       bit           NOT NULL CONSTRAINT [DF_CustomerLimits_AllowIfBet]    DEFAULT (1),
        [AllowReverse]     bit           NOT NULL CONSTRAINT [DF_CustomerLimits_AllowReverse]  DEFAULT (1),
        [AllowCasino]      bit           NOT NULL CONSTRAINT [DF_CustomerLimits_AllowCasino]   DEFAULT (1),
        [AllowLottery]     bit           NOT NULL CONSTRAINT [DF_CustomerLimits_AllowLottery]  DEFAULT (1),
        CONSTRAINT [PK_CustomerLimits] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_CustomerLimits_Customers_CustomerId]
            FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id])
            ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX [IX_CustomerLimits_CustomerId] ON [dbo].[CustomerLimits] ([CustomerId]);

    PRINT 'Created table CustomerLimits';
END
GO

-- ── Wagers ───────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Wagers')
BEGIN
    CREATE TABLE [dbo].[Wagers] (
        [Id]             int           NOT NULL IDENTITY(1,1),
        [CustomerId]     int           NOT NULL,
        [AgentId]        int           NOT NULL,
        [WagerType]      int           NOT NULL,
        [Status]         int           NOT NULL,
        [RiskAmount]     decimal(18,2) NOT NULL,
        [WinAmount]      decimal(18,2) NOT NULL,
        [ActualPayout]   decimal(18,2) NULL,
        [TicketNumber]   nvarchar(50)  NULL,
        [Notes]          nvarchar(500) NULL,
        [CreatedAt]      datetime2     NOT NULL,
        [GradedAt]       datetime2     NULL,
        [GradedBy]       nvarchar(100) NULL,
        [IdempotencyKey] nvarchar(100) NULL,
        CONSTRAINT [PK_Wagers] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Wagers_Customers_CustomerId]
            FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id])
            ON DELETE NO ACTION
    );

    CREATE INDEX [IX_Wagers_CustomerId] ON [dbo].[Wagers] ([CustomerId]);
    CREATE INDEX [IX_Wagers_CreatedAt]  ON [dbo].[Wagers] ([CreatedAt]);

    -- Filtered unique index: allows multiple NULLs, enforces uniqueness for non-NULL keys
    CREATE UNIQUE INDEX [IX_Wagers_IdempotencyKey]
        ON [dbo].[Wagers] ([IdempotencyKey])
        WHERE [IdempotencyKey] IS NOT NULL;

    PRINT 'Created table Wagers';
END
GO

-- ── Transactions ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Transactions')
BEGIN
    CREATE TABLE [dbo].[Transactions] (
        [Id]              int           NOT NULL IDENTITY(1,1),
        [CustomerId]      int           NOT NULL,
        [TransactionType] int           NOT NULL,
        [Amount]          decimal(18,2) NOT NULL,
        [BalanceBefore]   decimal(18,2) NOT NULL,
        [BalanceAfter]    decimal(18,2) NOT NULL,
        [Description]     nvarchar(500) NULL,
        [RelatedWagerId]  int           NULL,
        [CreatedBy]       nvarchar(100) NOT NULL,
        [CreatedAt]       datetime2     NOT NULL,
        CONSTRAINT [PK_Transactions] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Transactions_Customers_CustomerId]
            FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id])
            ON DELETE NO ACTION,
        CONSTRAINT [FK_Transactions_Wagers_RelatedWagerId]
            FOREIGN KEY ([RelatedWagerId]) REFERENCES [dbo].[Wagers] ([Id])
            ON DELETE NO ACTION
    );

    CREATE INDEX [IX_Transactions_CustomerId]     ON [dbo].[Transactions] ([CustomerId]);
    CREATE INDEX [IX_Transactions_CreatedAt]      ON [dbo].[Transactions] ([CreatedAt]);
    CREATE INDEX [IX_Transactions_RelatedWagerId] ON [dbo].[Transactions] ([RelatedWagerId]);

    PRINT 'Created table Transactions';
END
GO

-- ── LineSets ─────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LineSets')
BEGIN
    CREATE TABLE [dbo].[LineSets] (
        [Id]                 int          NOT NULL IDENTITY(1,1),
        [GamePeriodId]       int          NOT NULL,
        [Spread]             decimal(6,2) NULL,
        [SpreadJuice]        decimal(6,2) NULL,
        [HomeMoneyLine]      decimal(8,2) NULL,
        [AwayMoneyLine]      decimal(8,2) NULL,
        [Total]              decimal(6,2) NULL,
        [OverJuice]          decimal(6,2) NULL,
        [UnderJuice]         decimal(6,2) NULL,
        [OfferingSpread]     bit          NOT NULL CONSTRAINT [DF_LineSets_OfferingSpread]     DEFAULT (1),
        [OfferingMoneyLine]  bit          NOT NULL CONSTRAINT [DF_LineSets_OfferingMoneyLine]  DEFAULT (1),
        [OfferingTotal]      bit          NOT NULL CONSTRAINT [DF_LineSets_OfferingTotal]      DEFAULT (1),
        [LastModified]       datetime2    NOT NULL,
        [ModifiedBy]         nvarchar(100) NULL,
        CONSTRAINT [PK_LineSets] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_LineSets_GamePeriods_GamePeriodId]
            FOREIGN KEY ([GamePeriodId]) REFERENCES [dbo].[GamePeriods] ([Id])
            ON DELETE CASCADE
    );

    -- One LineSet per GamePeriod
    CREATE UNIQUE INDEX [IX_LineSets_GamePeriodId] ON [dbo].[LineSets] ([GamePeriodId]);

    PRINT 'Created table LineSets';
END
GO

-- ── WagerItems ───────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'WagerItems')
BEGIN
    CREATE TABLE [dbo].[WagerItems] (
        [Id]                  int          NOT NULL IDENTITY(1,1),
        [WagerId]             int          NOT NULL,
        [GamePeriodId]        int          NOT NULL,
        [ItemType]            int          NOT NULL,
        [Side]                int          NOT NULL,
        [LineAtTimeOfWager]   decimal(8,2) NOT NULL,
        [Status]              int          NOT NULL,
        [HomeScore]           int          NULL,
        [AwayScore]           int          NULL,
        CONSTRAINT [PK_WagerItems] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_WagerItems_Wagers_WagerId]
            FOREIGN KEY ([WagerId]) REFERENCES [dbo].[Wagers] ([Id])
            ON DELETE CASCADE,
        CONSTRAINT [FK_WagerItems_GamePeriods_GamePeriodId]
            FOREIGN KEY ([GamePeriodId]) REFERENCES [dbo].[GamePeriods] ([Id])
            ON DELETE NO ACTION
    );

    CREATE INDEX [IX_WagerItems_WagerId]      ON [dbo].[WagerItems] ([WagerId]);
    CREATE INDEX [IX_WagerItems_GamePeriodId] ON [dbo].[WagerItems] ([GamePeriodId]);

    PRINT 'Created table WagerItems';
END
GO

-- ── LineShades ───────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LineShades')
BEGIN
    CREATE TABLE [dbo].[LineShades] (
        [Id]                       int          NOT NULL IDENTITY(1,1),
        [LineSetId]                int          NOT NULL,
        [AgentId]                  int          NOT NULL,
        [SpreadAdjustment]         decimal(6,2) NULL,
        [HomeMoneyLineAdjustment]  decimal(8,2) NULL,
        [AwayMoneyLineAdjustment]  decimal(8,2) NULL,
        [TotalAdjustment]          decimal(6,2) NULL,
        [CreatedAt]                datetime2    NOT NULL,
        CONSTRAINT [PK_LineShades] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_LineShades_LineSets_LineSetId]
            FOREIGN KEY ([LineSetId]) REFERENCES [dbo].[LineSets] ([Id])
            ON DELETE CASCADE,
        CONSTRAINT [FK_LineShades_Agents_AgentId]
            FOREIGN KEY ([AgentId]) REFERENCES [dbo].[Agents] ([Id])
            ON DELETE CASCADE
    );

    CREATE INDEX [IX_LineShades_LineSetId] ON [dbo].[LineShades] ([LineSetId]);
    CREATE INDEX [IX_LineShades_AgentId]   ON [dbo].[LineShades] ([AgentId]);

    PRINT 'Created table LineShades';
END
GO

PRINT 'betting-service schema migration complete.';
GO
