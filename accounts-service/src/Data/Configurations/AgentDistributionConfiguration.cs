using AccountsService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountsService.Data.Configurations;

public class AgentDistributionConfiguration : IEntityTypeConfiguration<AgentDistribution>
{
    public void Configure(EntityTypeBuilder<AgentDistribution> builder)
    {
        builder.ToTable("AgentDistribution");
        builder.HasKey(d => d.Id);

        builder.HasIndex(d => new { d.AgentId, d.WeekEnding })
            .IsUnique()
            .HasDatabaseName("IX_AgentDistribution_AgentId_WeekEnding");

        builder.Property(d => d.WinAmount).HasPrecision(18, 2);
        builder.Property(d => d.LossAmount).HasPrecision(18, 2);
        builder.Property(d => d.NetAmount).HasPrecision(18, 2);
        builder.Property(d => d.CasinoWinAmount).HasPrecision(18, 2);
        builder.Property(d => d.CasinoLossAmount).HasPrecision(18, 2);
        builder.Property(d => d.CasinoFeeAmount).HasPrecision(18, 2);
        builder.Property(d => d.LiveDealerWin).HasPrecision(18, 2);
        builder.Property(d => d.LiveDealerLoss).HasPrecision(18, 2);
        builder.Property(d => d.LiveDealerFee).HasPrecision(18, 2);
        builder.Property(d => d.CreditAdjustments).HasPrecision(18, 2);
        builder.Property(d => d.DebitAdjustments).HasPrecision(18, 2);
        builder.Property(d => d.CommissionRate).HasPrecision(8, 4);
        builder.Property(d => d.CommissionAmount).HasPrecision(18, 2);
        builder.Property(d => d.PreviousMakeup).HasPrecision(18, 2);
        builder.Property(d => d.NewMakeup).HasPrecision(18, 2);
        builder.Property(d => d.HeadCountFee).HasPrecision(18, 2);
        builder.Property(d => d.NewBalance).HasPrecision(18, 2);

        builder.Property(d => d.CommissionType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(d => d.CalculatedBy).IsRequired().HasMaxLength(50);

        builder.HasOne(d => d.Agent)
            .WithMany()
            .HasForeignKey(d => d.AgentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
