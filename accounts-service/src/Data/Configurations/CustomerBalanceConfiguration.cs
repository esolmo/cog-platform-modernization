using AccountsService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountsService.Data.Configurations;

public class CustomerBalanceConfiguration : IEntityTypeConfiguration<CustomerBalance>
{
    public void Configure(EntityTypeBuilder<CustomerBalance> builder)
    {
        builder.ToTable("CustomerBalance");
        builder.HasKey(b => b.CustomerId);

        builder.Property(b => b.CreditLimit).HasPrecision(18, 2);
        builder.Property(b => b.WagerLimit).HasPrecision(18, 2);
        builder.Property(b => b.CurrentBalance).HasPrecision(18, 2);
        builder.Property(b => b.PendingWagerBalance).HasPrecision(18, 2);
        builder.Property(b => b.TempCreditAdjustment).HasPrecision(18, 2);
        builder.Property(b => b.FreePlayBalance).HasPrecision(18, 2);
        builder.Property(b => b.FreePlayPendingBalance).HasPrecision(18, 2);

        // AvailableCredit is a computed property — not mapped to DB column
        builder.Ignore(b => b.AvailableCredit);
    }
}
