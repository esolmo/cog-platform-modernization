using AccountsService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountsService.Data.Configurations;

public class CustomerCommentConfiguration : IEntityTypeConfiguration<CustomerComment>
{
    public void Configure(EntityTypeBuilder<CustomerComment> builder)
    {
        builder.ToTable("CustomerComment");
        builder.HasKey(c => c.Id);

        builder.HasIndex(c => c.CustomerId)
            .HasDatabaseName("IX_CustomerComment_CustomerId");

        builder.Property(c => c.Body).IsRequired().HasMaxLength(2000);
        builder.Property(c => c.CreatedBy).IsRequired().HasMaxLength(50);

        builder.HasOne(c => c.Customer)
            .WithMany(cu => cu.Comments)
            .HasForeignKey(c => c.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
