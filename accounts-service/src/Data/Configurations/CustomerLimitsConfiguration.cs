using AccountsService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountsService.Data.Configurations;

public class CustomerLimitsConfiguration : IEntityTypeConfiguration<CustomerLimits>
{
    public void Configure(EntityTypeBuilder<CustomerLimits> builder)
    {
        builder.ToTable("CustomerLimits");
        builder.HasKey(l => l.CustomerId);

        builder.Property(l => l.MaxStraightWager).HasPrecision(18, 2);
        builder.Property(l => l.MaxParlayWager).HasPrecision(18, 2);
        builder.Property(l => l.MaxParlayPayout).HasPrecision(18, 2);
        builder.Property(l => l.MaxTeaserWager).HasPrecision(18, 2);
        builder.Property(l => l.MaxIfBetWager).HasPrecision(18, 2);
        builder.Property(l => l.MaxLotteryPick3).HasPrecision(18, 2);
        builder.Property(l => l.MaxLotteryPick4).HasPrecision(18, 2);
        builder.Property(l => l.HardCreditLimit).HasPrecision(18, 2);
        builder.Property(l => l.CasinoWagerLimit).HasPrecision(18, 2);
        builder.Property(l => l.CasinoCreditLimit).HasPrecision(18, 2);
        builder.Property(l => l.SettleFigure).HasPrecision(18, 2);
    }
}
