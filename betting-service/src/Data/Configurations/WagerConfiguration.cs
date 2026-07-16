using Cog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BettingService.Data.Configurations;

public class WagerConfiguration : IEntityTypeConfiguration<Wager>
{
    public void Configure(EntityTypeBuilder<Wager> builder)
    {
        builder.ToTable("Wagers");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.RiskAmount).HasColumnType("decimal(18,2)");
        builder.Property(w => w.WinAmount).HasColumnType("decimal(18,2)");
        builder.Property(w => w.ActualPayout).HasColumnType("decimal(18,2)");
        builder.Property(w => w.TicketNumber).HasMaxLength(50);
        builder.Property(w => w.Notes).HasMaxLength(500);
        builder.Property(w => w.IdempotencyKey).HasMaxLength(100);

        builder.HasIndex(w => w.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");
        builder.HasIndex(w => w.CustomerId);
        builder.HasIndex(w => w.CreatedAt);

        builder.HasOne(w => w.Customer)
            .WithMany(c => c.Wagers)
            .HasForeignKey(w => w.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(w => w.Items)
            .WithOne(i => i.Wager)
            .HasForeignKey(i => i.WagerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
