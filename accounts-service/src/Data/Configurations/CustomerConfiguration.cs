using AccountsService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountsService.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customer");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("idCustomer").UseIdentityColumn();

        builder.Property(c => c.LoginName)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(c => c.AlternateLoginName)
            .HasMaxLength(20);

        builder.HasIndex(c => c.LoginName)
            .IsUnique()
            .HasDatabaseName("IX_Customer_LoginName");

        builder.Property(c => c.Email).HasMaxLength(200);
        builder.Property(c => c.Phone).HasMaxLength(50);
        builder.Property(c => c.CreatedBy).IsRequired().HasMaxLength(50);
        builder.Property(c => c.UpdatedBy).HasMaxLength(50);

        builder.Property(c => c.OddsFormat)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(c => c.Agent)
            .WithMany(a => a.Customers)
            .HasForeignKey(c => c.AgentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Balance)
            .WithOne(b => b.Customer)
            .HasForeignKey<CustomerBalance>(b => b.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Limits)
            .WithOne(l => l.Customer)
            .HasForeignKey<CustomerLimits>(l => l.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Transactions)
            .WithOne(t => t.Customer)
            .HasForeignKey(t => t.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Permissions)
            .WithOne(p => p.Customer)
            .HasForeignKey<CustomerPermissions>(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Comments)
            .WithOne(cm => cm.Customer)
            .HasForeignKey(cm => cm.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.FreePlays)
            .WithOne(f => f.Customer)
            .HasForeignKey(f => f.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
