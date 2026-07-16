-- =============================================================================
-- Seed: Admin Service Roles + Role-Permission Assignments
-- Must run AFTER 02_admin_permissions.sql
-- SuperAdmin and Admin are system roles (IsSystemRole = 1) — cannot be deleted
-- =============================================================================
SET NOCOUNT ON;

-- ─── System Roles ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'SuperAdmin')
    INSERT INTO Roles (Name, Description, IsSystemRole, IsActive)
    VALUES ('SuperAdmin', 'Full system access — all permissions granted', 1, 1);

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Admin')
    INSERT INTO Roles (Name, Description, IsSystemRole, IsActive)
    VALUES ('Admin', 'Administrative access — most permissions except system config', 1, 1);

-- ─── Operational Roles ────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'LinesManager')
    INSERT INTO Roles (Name, Description, IsSystemRole, IsActive)
    VALUES ('LinesManager', 'Can view and edit betting lines and odds', 0, 1);

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'TicketWriter')
    INSERT INTO Roles (Name, Description, IsSystemRole, IsActive)
    VALUES ('TicketWriter', 'Can create wagers and view accounts', 0, 1);

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'AgentManager')
    INSERT INTO Roles (Name, Description, IsSystemRole, IsActive)
    VALUES ('AgentManager', 'Can view and manage customer accounts and transactions', 0, 1);

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'ReportsViewer')
    INSERT INTO Roles (Name, Description, IsSystemRole, IsActive)
    VALUES ('ReportsViewer', 'Read-only access to all reports', 0, 1);

-- ─── SuperAdmin: all permissions ──────────────────────────────────────────────

INSERT INTO RolePermissions (RoleId, PermissionId)
SELECT r.Id, p.Id
FROM Roles r
CROSS JOIN Permissions p
WHERE r.Name = 'SuperAdmin'
  AND NOT EXISTS (
    SELECT 1 FROM RolePermissions rp
    WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
  );

-- ─── Admin: all except config.edit ────────────────────────────────────────────

INSERT INTO RolePermissions (RoleId, PermissionId)
SELECT r.Id, p.Id
FROM Roles r
CROSS JOIN Permissions p
WHERE r.Name = 'Admin'
  AND p.Name <> 'config.edit'
  AND NOT EXISTS (
    SELECT 1 FROM RolePermissions rp
    WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
  );

-- ─── LinesManager: lines.view + lines.edit + wagers.view ──────────────────────

INSERT INTO RolePermissions (RoleId, PermissionId)
SELECT r.Id, p.Id
FROM Roles r
CROSS JOIN Permissions p
WHERE r.Name = 'LinesManager'
  AND p.Name IN ('lines.view', 'lines.edit', 'wagers.view')
  AND NOT EXISTS (
    SELECT 1 FROM RolePermissions rp
    WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
  );

-- ─── TicketWriter: wagers.create + wagers.view + accounts.view ────────────────

INSERT INTO RolePermissions (RoleId, PermissionId)
SELECT r.Id, p.Id
FROM Roles r
CROSS JOIN Permissions p
WHERE r.Name = 'TicketWriter'
  AND p.Name IN ('wagers.create', 'wagers.view', 'accounts.view')
  AND NOT EXISTS (
    SELECT 1 FROM RolePermissions rp
    WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
  );

-- ─── AgentManager: accounts.view + accounts.edit + transactions.create ─────────

INSERT INTO RolePermissions (RoleId, PermissionId)
SELECT r.Id, p.Id
FROM Roles r
CROSS JOIN Permissions p
WHERE r.Name = 'AgentManager'
  AND p.Name IN ('accounts.view', 'accounts.edit', 'transactions.create', 'wagers.view')
  AND NOT EXISTS (
    SELECT 1 FROM RolePermissions rp
    WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
  );

-- ─── ReportsViewer: reports.view + wagers.view + accounts.view ────────────────

INSERT INTO RolePermissions (RoleId, PermissionId)
SELECT r.Id, p.Id
FROM Roles r
CROSS JOIN Permissions p
WHERE r.Name = 'ReportsViewer'
  AND p.Name IN ('reports.view', 'wagers.view', 'accounts.view')
  AND NOT EXISTS (
    SELECT 1 FROM RolePermissions rp
    WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
  );

PRINT 'Roles and role-permission assignments seeded.';
