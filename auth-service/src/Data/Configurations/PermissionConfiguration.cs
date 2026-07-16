using AuthService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Data.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Category).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.HasIndex(p => p.Name).IsUnique();

        builder.HasMany(p => p.RolePermissions)
            .WithOne(rp => rp.Permission)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed permissions
        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new Permission { Id = 1,  Name = PermissionNames.WagersCreate,    Category = "Wagers",   Description = "Create new wagers" },
            new Permission { Id = 2,  Name = PermissionNames.WagersRead,      Category = "Wagers",   Description = "View wagers" },
            new Permission { Id = 3,  Name = PermissionNames.WagersCancel,    Category = "Wagers",   Description = "Cancel pending wagers" },
            new Permission { Id = 4,  Name = PermissionNames.WagersGrade,     Category = "Wagers",   Description = "Grade/settle wagers" },
            new Permission { Id = 5,  Name = PermissionNames.LinesRead,       Category = "Lines",    Description = "View betting lines" },
            new Permission { Id = 6,  Name = PermissionNames.LinesWrite,      Category = "Lines",    Description = "Set spreads, moneylines, totals" },
            new Permission { Id = 7,  Name = PermissionNames.LinesShade,      Category = "Lines",    Description = "Apply/remove per-agent shades" },
            new Permission { Id = 8,  Name = PermissionNames.AccountsRead,    Category = "Accounts", Description = "View customer account details" },
            new Permission { Id = 9,  Name = PermissionNames.AccountsWrite,   Category = "Accounts", Description = "Edit customer account settings" },
            new Permission { Id = 10, Name = PermissionNames.AccountsDeposit, Category = "Accounts", Description = "Post deposit transactions" },
            new Permission { Id = 11, Name = PermissionNames.AccountsWithdraw,Category = "Accounts", Description = "Post withdrawal transactions" },
            new Permission { Id = 12, Name = PermissionNames.AgentsRead,      Category = "Agents",   Description = "View agent hierarchy" },
            new Permission { Id = 13, Name = PermissionNames.AgentsManage,    Category = "Agents",   Description = "Create and manage agents" },
            new Permission { Id = 14, Name = PermissionNames.ReportsView,     Category = "Reports",  Description = "View operational reports" },
            new Permission { Id = 15, Name = PermissionNames.ReportsExport,   Category = "Reports",  Description = "Export report data" },
            new Permission { Id = 16, Name = PermissionNames.UsersManage,     Category = "Admin",    Description = "Create and manage user accounts" },
            new Permission { Id = 17, Name = PermissionNames.RolesManage,     Category = "Admin",    Description = "Assign and revoke roles" },
            new Permission { Id = 18, Name = PermissionNames.SystemConfig,    Category = "Admin",    Description = "Change system configuration" },
            new Permission { Id = 19, Name = PermissionNames.LotteryManage,   Category = "Lottery",  Description = "Manage lottery draws and results" },
            new Permission { Id = 20, Name = PermissionNames.LotteryPlay,     Category = "Lottery",  Description = "Purchase lottery tickets" }
        );
    }
}
