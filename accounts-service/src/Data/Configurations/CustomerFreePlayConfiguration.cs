using AccountsService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountsService.Data.Configurations;

public class CustomerFreePlayConfiguration : IEntityTypeConfiguration<CustomerFreePlay>
{
    public void Configure(EntityTypeBuilder<CustomerFreePlay> builder)
    {
        builder.ToTable("CustomerFreePlay");
        builder.HasKey(f => f.Id);

        builder.HasIndex(f => f.CustomerId)
            .HasDatabaseName("IX_CustomerFreePlay_CustomerId");

        builder.Property(f => f.Amount).HasPrecision(18, 2);
        builder.Property(f => f.RedeemedAmount).HasPrecision(18, 2);
        builder.Property(f => f.Description).IsRequired().HasMaxLength(200);
        builder.Property(f => f.IssuedBy).IsRequired().HasMaxLength(50);

        builder.HasOne(f => f.Customer)
            .WithMany(c => c.FreePlays)
            .HasForeignKey(f => f.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
