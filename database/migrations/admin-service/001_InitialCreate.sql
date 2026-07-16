-- =============================================================================
-- admin-service: Initial Schema
-- Replaces: Admin/ Delphi desktop + LoginsRoles/ Delphi desktop
-- Permissions map LegacyBitValue → named permission for migration
-- Run order: 001
-- =============================================================================
SET NOCOUNT ON;
GO

-- ─── Application Users ────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ApplicationUsers')
CREATE TABLE ApplicationUsers (
    Id              INT            NOT NULL IDENTITY PRIMARY KEY,
    Username        NVARCHAR(100)  NOT NULL,
    Email           NVARCHAR(200)  NOT NULL,
    PasswordHash    NVARCHAR(256)  NOT NULL,
    FirstName       NVARCHAR(100)  NULL,
    LastName        NVARCHAR(100)  NULL,
    MaxAccessLevel  INT            NOT NULL DEFAULT 0,
    IsActive        BIT            NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    LastLoginAt     DATETIME2      NULL
);
GO

-- ─── Roles ────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Roles')
CREATE TABLE Roles (
    Id          INT            NOT NULL IDENTITY PRIMARY KEY,
    Name        NVARCHAR(100)  NOT NULL,
    Description NVARCHAR(500)  NULL,
    IsSystemRole BIT           NOT NULL DEFAULT 0,  -- SuperAdmin / Admin cannot be deleted
    IsActive    BIT            NOT NULL DEFAULT 1,
    CreatedAt   DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

-- ─── Permissions ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Permissions')
CREATE TABLE Permissions (
    Id              INT            NOT NULL IDENTITY PRIMARY KEY,
    Name            NVARCHAR(100)  NOT NULL,  -- e.g. 'users.view'
    Category        NVARCHAR(50)   NOT NULL,  -- e.g. 'Users', 'Wagers'
    Description     NVARCHAR(500)  NULL,
    LegacyBitValue  INT            NULL       -- Maps to legacy SecurityLevel enum value
);
GO

-- ─── User Roles (join) ────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserRoles')
CREATE TABLE UserRoles (
    UserId  INT NOT NULL,
    RoleId  INT NOT NULL,
    PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_UserRoles_User FOREIGN KEY (UserId) REFERENCES ApplicationUsers(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserRoles_Role FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
);
GO

-- ─── Role Permissions (join) ─────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RolePermissions')
CREATE TABLE RolePermissions (
    RoleId       INT NOT NULL,
    PermissionId INT NOT NULL,
    PRIMARY KEY (RoleId, PermissionId),
    CONSTRAINT FK_RolePermissions_Role       FOREIGN KEY (RoleId)       REFERENCES Roles(Id)       ON DELETE CASCADE,
    CONSTRAINT FK_RolePermissions_Permission FOREIGN KEY (PermissionId) REFERENCES Permissions(Id) ON DELETE CASCADE
);
GO

-- ─── System Configuration ────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SystemConfigurations')
CREATE TABLE SystemConfigurations (
    Id                INT            NOT NULL IDENTITY PRIMARY KEY,
    [Key]             NVARCHAR(200)  NOT NULL,
    [Value]           NVARCHAR(MAX)  NULL,
    Category          NVARCHAR(100)  NULL,
    Description       NVARCHAR(500)  NULL,
    IsEncrypted       BIT            NOT NULL DEFAULT 0,
    UpdatedAt         DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedByUserId   INT            NULL,
    CONSTRAINT FK_SystemConfigurations_User FOREIGN KEY (UpdatedByUserId) REFERENCES ApplicationUsers(Id)
);
GO

-- ─── Audit Log ───────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AuditLogs')
CREATE TABLE AuditLogs (
    Id              BIGINT         NOT NULL IDENTITY PRIMARY KEY,
    UserId          INT            NULL,
    Username        NVARCHAR(100)  NOT NULL,
    Action          NVARCHAR(100)  NOT NULL,  -- Create, Update, Delete, Login, etc.
    EntityType      NVARCHAR(100)  NULL,
    EntityId        NVARCHAR(100)  NULL,
    OldValues       NVARCHAR(MAX)  NULL,      -- JSON snapshot before change
    NewValues       NVARCHAR(MAX)  NULL,      -- JSON snapshot after change
    IpAddress       NVARCHAR(50)   NULL,
    OccurredAt      DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

-- ─── Indexes ─────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ApplicationUsers_Username')
    CREATE UNIQUE INDEX IX_ApplicationUsers_Username ON ApplicationUsers(Username);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ApplicationUsers_Email')
    CREATE UNIQUE INDEX IX_ApplicationUsers_Email ON ApplicationUsers(Email);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Roles_Name')
    CREATE UNIQUE INDEX IX_Roles_Name ON Roles(Name);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Permissions_Name')
    CREATE UNIQUE INDEX IX_Permissions_Name ON Permissions(Name);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SystemConfigurations_Key')
    CREATE UNIQUE INDEX IX_SystemConfigurations_Key ON SystemConfigurations([Key]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditLogs_UserId_OccurredAt')
    CREATE INDEX IX_AuditLogs_UserId_OccurredAt ON AuditLogs(UserId, OccurredAt DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditLogs_EntityType_EntityId')
    CREATE INDEX IX_AuditLogs_EntityType_EntityId ON AuditLogs(EntityType, EntityId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditLogs_OccurredAt')
    CREATE INDEX IX_AuditLogs_OccurredAt ON AuditLogs(OccurredAt DESC);
GO
