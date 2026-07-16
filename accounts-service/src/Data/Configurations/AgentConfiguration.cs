using AccountsService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountsService.Data.Configurations;

public class AgentConfiguration : IEntityTypeConfiguration<Agent>
{
    public void Configure(EntityTypeBuilder<Agent> builder)
    {
        builder.ToTable("Agent");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("idAgent");

        builder.Property(a => a.LoginName)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasIndex(a => a.LoginName)
            .IsUnique()
            .HasDatabaseName("IX_Agent_LoginName");

        builder.Property(a => a.Name).HasMaxLength(100);
        builder.Property(a => a.CreatedBy).IsRequired().HasMaxLength(50);

        builder.Property(a => a.AgentType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.CommissionType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(a => a.CreditLimitMax).HasPrecision(18, 2);
        builder.Property(a => a.WagerLimitMax).HasPrecision(18, 2);
        builder.Property(a => a.CommissionRate).HasPrecision(8, 4);

        // Self-referencing hierarchy: Agent → ParentAgent
        builder.HasOne(a => a.ParentAgent)
            .WithMany(a => a.SubAgents)
            .HasForeignKey(a => a.ParentAgentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
