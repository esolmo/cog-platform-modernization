-- =============================================================================
-- auth-service: Initial Schema
-- Mirrors EF Core / ASP.NET Core Identity schema for SQL Server
-- Run order: 001
-- =============================================================================
SET NOCOUNT ON;
GO

-- ─── ASP.NET Core Identity tables ────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetRoles')
CREATE TABLE AspNetRoles (
    Id               NVARCHAR(450)  NOT NULL PRIMARY KEY,
    Name             NVARCHAR(256)  NULL,
    NormalizedName   NVARCHAR(256)  NULL,
    ConcurrencyStamp NVARCHAR(MAX)  NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUsers')
CREATE TABLE AspNetUsers (
    Id                   NVARCHAR(450)  NOT NULL PRIMARY KEY,
    UserName             NVARCHAR(256)  NULL,
    NormalizedUserName   NVARCHAR(256)  NULL,
    Email                NVARCHAR(256)  NULL,
    NormalizedEmail      NVARCHAR(256)  NULL,
    EmailConfirmed       BIT            NOT NULL DEFAULT 0,
    PasswordHash         NVARCHAR(MAX)  NULL,
    SecurityStamp        NVARCHAR(MAX)  NULL,
    ConcurrencyStamp     NVARCHAR(MAX)  NULL,
    PhoneNumber          NVARCHAR(MAX)  NULL,
    PhoneNumberConfirmed BIT            NOT NULL DEFAULT 0,
    TwoFactorEnabled     BIT            NOT NULL DEFAULT 0,
    LockoutEnd           DATETIMEOFFSET NULL,
    LockoutEnabled       BIT            NOT NULL DEFAULT 0,
    AccessFailedCount    INT            NOT NULL DEFAULT 0,
    -- COG extensions
    AgentId              INT            NULL,
    MaxAccessLevel       INT            NOT NULL DEFAULT 0,
    CreatedAt            DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    LastLoginAt          DATETIME2      NULL,
    IsActive             BIT            NOT NULL DEFAULT 1
);
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUserRoles')
CREATE TABLE AspNetUserRoles (
    UserId NVARCHAR(450) NOT NULL,
    RoleId NVARCHAR(450) NOT NULL,
    PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_AspNetUserRoles_User FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    CONSTRAINT FK_AspNetUserRoles_Role FOREIGN KEY (RoleId) REFERENCES AspNetRoles(Id)  ON DELETE CASCADE
);
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUserClaims')
CREATE TABLE AspNetUserClaims (
    Id         INT            NOT NULL IDENTITY PRIMARY KEY,
    UserId     NVARCHAR(450)  NOT NULL,
    ClaimType  NVARCHAR(MAX)  NULL,
    ClaimValue NVARCHAR(MAX)  NULL,
    CONSTRAINT FK_AspNetUserClaims_User FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetRoleClaims')
CREATE TABLE AspNetRoleClaims (
    Id         INT            NOT NULL IDENTITY PRIMARY KEY,
    RoleId     NVARCHAR(450)  NOT NULL,
    ClaimType  NVARCHAR(MAX)  NULL,
    ClaimValue NVARCHAR(MAX)  NULL,
    CONSTRAINT FK_AspNetRoleClaims_Role FOREIGN KEY (RoleId) REFERENCES AspNetRoles(Id) ON DELETE CASCADE
);
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUserLogins')
CREATE TABLE AspNetUserLogins (
    LoginProvider       NVARCHAR(128) NOT NULL,
    ProviderKey         NVARCHAR(128) NOT NULL,
    ProviderDisplayName NVARCHAR(MAX) NULL,
    UserId              NVARCHAR(450) NOT NULL,
    PRIMARY KEY (LoginProvider, ProviderKey),
    CONSTRAINT FK_AspNetUserLogins_User FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUserTokens')
CREATE TABLE AspNetUserTokens (
    UserId        NVARCHAR(450) NOT NULL,
    LoginProvider NVARCHAR(128) NOT NULL,
    Name          NVARCHAR(128) NOT NULL,
    Value         NVARCHAR(MAX) NULL,
    PRIMARY KEY (UserId, LoginProvider, Name),
    CONSTRAINT FK_AspNetUserTokens_User FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);
GO

-- ─── Refresh token store ─────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RefreshTokens')
CREATE TABLE RefreshTokens (
    Id          BIGINT        NOT NULL IDENTITY PRIMARY KEY,
    UserId      NVARCHAR(450) NOT NULL,
    Token       NVARCHAR(512) NOT NULL,
    ExpiresAt   DATETIME2     NOT NULL,
    IsRevoked   BIT           NOT NULL DEFAULT 0,
    CreatedAt   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    RevokedAt   DATETIME2     NULL,
    ReplacedBy  NVARCHAR(512) NULL,
    CONSTRAINT FK_RefreshTokens_User FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);
GO

-- ─── Indexes ──────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AspNetUsers_NormalizedUserName')
    CREATE UNIQUE INDEX IX_AspNetUsers_NormalizedUserName ON AspNetUsers(NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AspNetUsers_NormalizedEmail')
    CREATE INDEX IX_AspNetUsers_NormalizedEmail ON AspNetUsers(NormalizedEmail);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AspNetRoles_NormalizedName')
    CREATE UNIQUE INDEX IX_AspNetRoles_NormalizedName ON AspNetRoles(NormalizedName) WHERE NormalizedName IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RefreshTokens_Token')
    CREATE UNIQUE INDEX IX_RefreshTokens_Token ON RefreshTokens(Token);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RefreshTokens_UserId')
    CREATE INDEX IX_RefreshTokens_UserId ON RefreshTokens(UserId);
GO
