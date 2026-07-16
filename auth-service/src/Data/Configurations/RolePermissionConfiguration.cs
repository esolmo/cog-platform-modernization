using AuthService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Data.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        // Seed role-permission mappings (migrated from TbFssPermissionRoles)
        // RoleId => PermissionId references from RoleConfiguration and PermissionConfiguration seeds
        builder.HasData(
            // MasterAgent (1) — all permissions
            new RolePermission { RoleId = 1, PermissionId = 1  },
            new RolePermission { RoleId = 1, PermissionId = 2  },
            new RolePermission { RoleId = 1, PermissionId = 3  },
            new RolePermission { RoleId = 1, PermissionId = 4  },
            new RolePermission { RoleId = 1, PermissionId = 5  },
            new RolePermission { RoleId = 1, PermissionId = 6  },
            new RolePermission { RoleId = 1, PermissionId = 7  },
            new RolePermission { RoleId = 1, PermissionId = 8  },
            new RolePermission { RoleId = 1, PermissionId = 9  },
            new RolePermission { RoleId = 1, PermissionId = 10 },
            new RolePermission { RoleId = 1, PermissionId = 11 },
            new RolePermission { RoleId = 1, PermissionId = 12 },
            new RolePermission { RoleId = 1, PermissionId = 13 },
            new RolePermission { RoleId = 1, PermissionId = 14 },
            new RolePermission { RoleId = 1, PermissionId = 15 },

            // Agent (2) — wagers, accounts, lines read, reports view
            new RolePermission { RoleId = 2, PermissionId = 1  },
            new RolePermission { RoleId = 2, PermissionId = 2  },
            new RolePermission { RoleId = 2, PermissionId = 3  },
            new RolePermission { RoleId = 2, PermissionId = 5  },
            new RolePermission { RoleId = 2, PermissionId = 8  },
            new RolePermission { RoleId = 2, PermissionId = 9  },
            new RolePermission { RoleId = 2, PermissionId = 10 },
            new RolePermission { RoleId = 2, PermissionId = 11 },
            new RolePermission { RoleId = 2, PermissionId = 12 },
            new RolePermission { RoleId = 2, PermissionId = 14 },

            // SubAgent (3) — create/read wagers, view accounts/lines
            new RolePermission { RoleId = 3, PermissionId = 1  },
            new RolePermission { RoleId = 3, PermissionId = 2  },
            new RolePermission { RoleId = 3, PermissionId = 5  },
            new RolePermission { RoleId = 3, PermissionId = 8  },

            // LinesManager (4) — all lines + reports
            new RolePermission { RoleId = 4, PermissionId = 5  },
            new RolePermission { RoleId = 4, PermissionId = 6  },
            new RolePermission { RoleId = 4, PermissionId = 7  },
            new RolePermission { RoleId = 4, PermissionId = 14 },
            new RolePermission { RoleId = 4, PermissionId = 15 },

            // Admin (5) — all permissions
            new RolePermission { RoleId = 5, PermissionId = 1  },
            new RolePermission { RoleId = 5, PermissionId = 2  },
            new RolePermission { RoleId = 5, PermissionId = 3  },
            new RolePermission { RoleId = 5, PermissionId = 4  },
            new RolePermission { RoleId = 5, PermissionId = 5  },
            new RolePermission { RoleId = 5, PermissionId = 6  },
            new RolePermission { RoleId = 5, PermissionId = 7  },
            new RolePermission { RoleId = 5, PermissionId = 8  },
            new RolePermission { RoleId = 5, PermissionId = 9  },
            new RolePermission { RoleId = 5, PermissionId = 10 },
            new RolePermission { RoleId = 5, PermissionId = 11 },
            new RolePermission { RoleId = 5, PermissionId = 12 },
            new RolePermission { RoleId = 5, PermissionId = 13 },
            new RolePermission { RoleId = 5, PermissionId = 14 },
            new RolePermission { RoleId = 5, PermissionId = 15 },
            new RolePermission { RoleId = 5, PermissionId = 16 },
            new RolePermission { RoleId = 5, PermissionId = 17 },
            new RolePermission { RoleId = 5, PermissionId = 18 },

            // CustomerWeb (6) — create wagers, view own account, play lottery
            new RolePermission { RoleId = 6, PermissionId = 1  },
            new RolePermission { RoleId = 6, PermissionId = 2  },
            new RolePermission { RoleId = 6, PermissionId = 5  },
            new RolePermission { RoleId = 6, PermissionId = 20 },

            // ReportsViewer (7)
            new RolePermission { RoleId = 7, PermissionId = 14 },
            new RolePermission { RoleId = 7, PermissionId = 15 },

            // LotteryManager (8)
            new RolePermission { RoleId = 8, PermissionId = 19 }
        );
    }
}
