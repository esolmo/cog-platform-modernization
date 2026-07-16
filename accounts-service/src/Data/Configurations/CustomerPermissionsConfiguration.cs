using AccountsService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountsService.Data.Configurations;

public class CustomerPermissionsConfiguration : IEntityTypeConfiguration<CustomerPermissions>
{
    public void Configure(EntityTypeBuilder<CustomerPermissions> builder)
    {
        builder.ToTable("CustomerPermissions");
        builder.HasKey(p => p.CustomerId);

        builder.Property(p => p.UpdatedBy).IsRequired().HasMaxLength(50);

        builder.HasOne(p => p.Customer)
            .WithOne(c => c.Permissions)
            .HasForeignKey<CustomerPermissions>(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
