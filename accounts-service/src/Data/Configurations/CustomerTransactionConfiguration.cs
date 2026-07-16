using AccountsService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountsService.Data.Configurations;

public class CustomerTransactionConfiguration : IEntityTypeConfiguration<CustomerTransaction>
{
    public void Configure(EntityTypeBuilder<CustomerTransaction> builder)
    {
        builder.ToTable("CustomerTransaction");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).UseIdentityColumn();

        builder.Property(t => t.Amount).HasPrecision(18, 2);
        builder.Property(t => t.BalanceBefore).HasPrecision(18, 2);
        builder.Property(t => t.BalanceAfter).HasPrecision(18, 2);

        builder.Property(t => t.Code)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(t => t.Type)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(t => t.Description).HasMaxLength(255);
        builder.Property(t => t.Reference).HasMaxLength(50);
        builder.Property(t => t.PaymentMethod).HasMaxLength(50);
        builder.Property(t => t.EnteredBy).IsRequired().HasMaxLength(50);

        builder.HasIndex(t => t.CustomerId)
            .HasDatabaseName("IX_CustomerTransaction_CustomerId");

        builder.HasIndex(t => t.TransactionDate)
            .HasDatabaseName("IX_CustomerTransaction_Date");
    }
}
