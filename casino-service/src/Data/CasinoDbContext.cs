using CasinoService.Entities;
using Microsoft.EntityFrameworkCore;

namespace CasinoService.Data;

public class CasinoDbContext(DbContextOptions<CasinoDbContext> options) : DbContext(options)
{
    public DbSet<CasinoPlayer> CasinoPlayers => Set<CasinoPlayer>();
    public DbSet<CasinoTransaction> CasinoTransactions => Set<CasinoTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CasinoPlayer>(b =>
        {
            b.ToTable("CasinoPlayers");
            b.HasKey(p => p.Id);
            b.HasIndex(p => new { p.CustomerId, p.CasinoId }).IsUnique();
            b.Property(p => p.CustomerId).HasMaxLength(50).IsRequired();
            b.Property(p => p.Nickname).HasMaxLength(100).IsRequired();
            b.Property(p => p.ExternalPlayerId).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<CasinoTransaction>(b =>
        {
            b.ToTable("CasinoTransactions");
            b.HasKey(t => t.Id);
            b.HasIndex(t => t.DocumentNumber).IsUnique();
            b.HasIndex(t => t.CasinoPlayerId);
            b.Property(t => t.Amount).HasPrecision(18, 2);
            b.Property(t => t.TransferReference).HasMaxLength(100);
            b.Property(t => t.RemoteReference).HasMaxLength(100);
            b.HasOne(t => t.Player)
             .WithMany(p => p.Transactions)
             .HasForeignKey(t => t.CasinoPlayerId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
