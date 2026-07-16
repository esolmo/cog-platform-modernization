using Cog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BettingService.Data.Configurations;

public class LineSetConfiguration : IEntityTypeConfiguration<LineSet>
{
    public void Configure(EntityTypeBuilder<LineSet> builder)
    {
        builder.ToTable("LineSets");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Spread).HasColumnType("decimal(6,2)");
        builder.Property(l => l.SpreadJuice).HasColumnType("decimal(6,2)");
        builder.Property(l => l.HomeMoneyLine).HasColumnType("decimal(8,2)");
        builder.Property(l => l.AwayMoneyLine).HasColumnType("decimal(8,2)");
        builder.Property(l => l.Total).HasColumnType("decimal(6,2)");
        builder.Property(l => l.OverJuice).HasColumnType("decimal(6,2)");
        builder.Property(l => l.UnderJuice).HasColumnType("decimal(6,2)");
        builder.Property(l => l.ModifiedBy).HasMaxLength(100);

        builder.HasOne(l => l.GamePeriod)
            .WithOne(gp => gp.LineSet)
            .HasForeignKey<LineSet>(l => l.GamePeriodId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(l => l.Shades)
            .WithOne(s => s.LineSet)
            .HasForeignKey(s => s.LineSetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
