using AdminService.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdminService.Data;

public class AdminDbContext(DbContextOptions<AdminDbContext> options) : DbContext(options)
{
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<SystemConfiguration> SystemConfigurations => Set<SystemConfiguration>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(b =>
        {
            b.ToTable("ApplicationUsers");
            b.HasKey(u => u.Id);
            b.HasIndex(u => u.Username).IsUnique();
            b.HasIndex(u => u.Email);
            b.Property(u => u.Username).HasMaxLength(50);
            b.Property(u => u.Email).HasMaxLength(200);
            b.Property(u => u.FirstName).HasMaxLength(100);
            b.Property(u => u.LastName).HasMaxLength(100);
            b.Property(u => u.MaxAccessLevel).HasMaxLength(50);
        });

        modelBuilder.Entity<Role>(b =>
        {
            b.HasKey(r => r.Id);
            b.HasIndex(r => r.Name).IsUnique();
            b.Property(r => r.Name).HasMaxLength(100);
            b.Property(r => r.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<Permission>(b =>
        {
            b.HasKey(p => p.Id);
            b.HasIndex(p => p.Name).IsUnique();
            b.Property(p => p.Name).HasMaxLength(100);
            b.Property(p => p.Category).HasMaxLength(50);
            b.Property(p => p.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<UserRole>(b =>
        {
            b.HasKey(ur => new { ur.UserId, ur.RoleId });
            b.HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RolePermission>(b =>
        {
            b.HasKey(rp => new { rp.RoleId, rp.PermissionId });
            b.HasOne(rp => rp.Role).WithMany(r => r.RolePermissions).HasForeignKey(rp => rp.RoleId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(rp => rp.Permission).WithMany(p => p.RolePermissions).HasForeignKey(rp => rp.PermissionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SystemConfiguration>(b =>
        {
            b.HasKey(c => c.Id);
            b.HasIndex(c => c.Key).IsUnique();
            b.Property(c => c.Key).HasMaxLength(200);
            b.Property(c => c.Category).HasMaxLength(100);
        });

        modelBuilder.Entity<AuditLog>(b =>
        {
            b.HasKey(a => a.Id);
            b.HasIndex(a => a.UserId);
            b.HasIndex(a => a.OccurredAt);
            b.HasIndex(a => new { a.EntityType, a.EntityId });
            b.Property(a => a.Action).HasMaxLength(100);
            b.Property(a => a.EntityType).HasMaxLength(100);
            b.Property(a => a.EntityId).HasMaxLength(50);
            b.Property(a => a.IpAddress).HasMaxLength(50);
            b.HasOne(a => a.User).WithMany(u => u.AuditLogs).HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed system roles
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "SuperAdmin", Description = "Full system access", IsActive = true, IsSystemRole = true },
            new Role { Id = 2, Name = "Admin", Description = "Administrative access", IsActive = true, IsSystemRole = true },
            new Role { Id = 3, Name = "Supervisor", Description = "Supervisory access — view all, limited edit", IsActive = true, IsSystemRole = false },
            new Role { Id = 4, Name = "LinesMaker", Description = "Manage betting lines and odds", IsActive = true, IsSystemRole = false },
            new Role { Id = 5, Name = "Cashier", Description = "Process transactions", IsActive = true, IsSystemRole = false },
            new Role { Id = 6, Name = "ReadOnly", Description = "View-only access", IsActive = true, IsSystemRole = false }
        );

        // Seed core permissions (replacing BitPermission bitmask constants from Constants.asp)
        modelBuilder.Entity<Permission>().HasData(
            new Permission { Id = 1,  Name = "users.view",            Category = "Users",      Description = "View system users",             LegacyBitValue = 0 },
            new Permission { Id = 2,  Name = "users.create",          Category = "Users",      Description = "Create system users",           LegacyBitValue = 0 },
            new Permission { Id = 3,  Name = "users.edit",            Category = "Users",      Description = "Edit system users",             LegacyBitValue = 0 },
            new Permission { Id = 4,  Name = "users.delete",          Category = "Users",      Description = "Delete system users",           LegacyBitValue = 0 },
            new Permission { Id = 5,  Name = "roles.manage",          Category = "Roles",      Description = "Manage roles and permissions",  LegacyBitValue = 0 },
            new Permission { Id = 6,  Name = "wagers.view",           Category = "Wagers",     Description = "View wagers",                   LegacyBitValue = 1 },
            new Permission { Id = 7,  Name = "wagers.create",         Category = "Wagers",     Description = "Create wagers",                 LegacyBitValue = 2 },
            new Permission { Id = 8,  Name = "wagers.void",           Category = "Wagers",     Description = "Void wagers",                   LegacyBitValue = 4 },
            new Permission { Id = 9,  Name = "lines.view",            Category = "Lines",      Description = "View betting lines",            LegacyBitValue = 8 },
            new Permission { Id = 10, Name = "lines.edit",            Category = "Lines",      Description = "Edit betting lines",            LegacyBitValue = 16 },
            new Permission { Id = 11, Name = "accounts.view",         Category = "Accounts",   Description = "View customer accounts",        LegacyBitValue = 32 },
            new Permission { Id = 12, Name = "accounts.edit",         Category = "Accounts",   Description = "Edit customer accounts",        LegacyBitValue = 64 },
            new Permission { Id = 13, Name = "transactions.create",   Category = "Accounts",   Description = "Create financial transactions", LegacyBitValue = 128 },
            new Permission { Id = 14, Name = "reports.view",          Category = "Reports",    Description = "View reports",                  LegacyBitValue = 256 },
            new Permission { Id = 15, Name = "config.view",           Category = "System",     Description = "View system configuration",     LegacyBitValue = 512 },
            new Permission { Id = 16, Name = "config.edit",           Category = "System",     Description = "Edit system configuration",     LegacyBitValue = 1024 },
            new Permission { Id = 17, Name = "audit.view",            Category = "System",     Description = "View audit logs",               LegacyBitValue = 2048 }
        );
    }
}
