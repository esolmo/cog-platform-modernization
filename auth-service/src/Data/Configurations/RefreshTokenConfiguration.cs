using AuthService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.TokenHash).HasMaxLength(256).IsRequired();
        builder.Property(rt => rt.RevokedReason).HasMaxLength(200);
        builder.Property(rt => rt.CreatedByIp).HasMaxLength(50);
        builder.Property(rt => rt.ReplacedByTokenHash).HasMaxLength(256);

        builder.HasIndex(rt => rt.TokenHash);
        builder.HasIndex(rt => new { rt.UserId, rt.IsRevoked });
    }
}
