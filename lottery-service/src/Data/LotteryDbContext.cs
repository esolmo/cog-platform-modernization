using LotteryService.Entities;
using Microsoft.EntityFrameworkCore;

namespace LotteryService.Data;

public class LotteryDbContext(DbContextOptions<LotteryDbContext> options) : DbContext(options)
{
    public DbSet<LotteryGame> LotteryGames => Set<LotteryGame>();
    public DbSet<DrawingDetail> DrawingDetails => Set<DrawingDetail>();
    public DbSet<LotteryTicket> LotteryTickets => Set<LotteryTicket>();
    public DbSet<LotteryPickEntry> LotteryPickEntries => Set<LotteryPickEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LotteryGame>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasData(
                new LotteryGame { Id = 1, GameType = LotteryGameType.Pick3, Name = "Pick 3" },
                new LotteryGame { Id = 2, GameType = LotteryGameType.Pick4, Name = "Pick 4" }
            );
        });

        modelBuilder.Entity<DrawingDetail>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.TimeZoneId).HasMaxLength(100);
            e.HasOne(x => x.LotteryGame)
             .WithMany(x => x.Drawings)
             .HasForeignKey(x => x.LotteryGameId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LotteryTicket>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Total).HasColumnType("decimal(18,2)");
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasOne(x => x.DrawingDetail)
             .WithMany(x => x.Tickets)
             .HasForeignKey(x => x.DrawingDetailId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LotteryPickEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.Property(x => x.Cost).HasColumnType("decimal(18,2)");
            e.Property(x => x.Prize).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Ticket)
             .WithMany(x => x.Picks)
             .HasForeignKey(x => x.TicketId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
