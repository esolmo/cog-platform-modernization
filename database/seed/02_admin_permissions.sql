-- =============================================================================
-- Seed: Admin Service Permissions
-- 17 named permissions replacing legacy BitPermission bitmask system
-- LegacyBitValue maps to the SecurityLevel enum from Constants.asp / DAReports.cs
-- =============================================================================
SET NOCOUNT ON;

-- Users category
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'users.view')
    INSERT INTO Permissions (Name, Category, Description, LegacyBitValue)
    VALUES ('users.view', 'Users', 'View operator users and their roles', 54);

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'users.create')
    INSERT INTO Permissions (Name, Category, Description, LegacyBitValue)
    VALUES ('users.create', 'Users', 'Create new operator users', 45);

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'users.edit')
    INSERT INTO Permissions (Name, Category, Description, LegacyBitValue)
    VALUES ('users.edit', 'Users', 'Edit existing operator users', 35);

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'users.delete')
    INSERT INTO Permissions (Name, Category, Description, LegacyBitValue)
    VALUES ('users.delete', 'Users', 'Deactivate (soft-delete) operator users', 7);

-- Roles category
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'roles.manage')
    INSERT INTO Permissions (Name, Category, Description, LegacyBitValue)
    VALUES ('roles.manage', 'Roles', 'Create, edit and delete roles and permission assignments', 53);

-- Wagers category
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'wagers.view')
    INSERT INTO Permissions (Name, Category, Description, LegacyBitValue)
    VALUES ('wagers.view', 'Wagers', 'View wager tickets and history', 24);

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'wagers.create')
    INSERT INTO Permissions (Name, Category, Description, LegacyBitValue)
    VALUES ('wagers.create', 'Wagers', 'Accept and create new wagers', 3);

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'wagers.void')
    INSERT INTO Permissions (Name, Category, Description, LegacyBitValue)
    VALUES ('wagers.void', 'Wagers', 'Void / delete wager tickets', 8);

-- Lines category
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'lines.view')
    INSERT INTO Permissions (Name, Category, Description, LegacyBitValue)
    VALUES ('lines.view', 'Lines', 'View betting lines and odds', 31);

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'lines.edit')
    INSERT INTO Permissions (Name, Category, Description, LegacyBitValue)
    VALUES ('lines.edit', 'Lines', 'Set and change betting lines and odds', 27);

-- Accounts category
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'accounts.view')
    INSERT INTO Permissions (Name, Category, Description, LegacyBitValue)
    VALUES ('accounts.view', 'Accounts', 'View customer account details', 47);

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'accounts.edit')
    INSERT INTO Permissions (Name, Category, Description, LegacyBitValue)
    VALUES ('accounts.edit', 'Accounts', 'Modify customer accounts and limits', 14);

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'transactions.create')
    INSERT INTO Permissions (Name, Category, Description, LegacyBitValue)
    VALUES ('transactions.create', 'Accounts', 'Enter customer transactions (deposits/withdrawals)', 13);

-- Reports category
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'reports.view')
    INSERT INTO Permissions (Name, Category, Description, LegacyBitValue)
    VALUES ('reports.view', 'Reports', 'Run and view reports', 51);

-- System category
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'config.view')
    INSERT INTO Permissions (Name, Category, Description, LegacyBitValue)
    VALUES ('config.view', 'System', 'View system configuration settings', 6);

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'config.edit')
    INSERT INTO Permissions (Name, Category, Description, LegacyBitValue)
    VALUES ('config.edit', 'System', 'Change system configuration settings', 41);

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'audit.view')
    INSERT INTO Permissions (Name, Category, Description, LegacyBitValue)
    VALUES ('audit.view', 'System', 'View system audit log', 25);

PRINT 'Permissions seeded: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' rows affected';
