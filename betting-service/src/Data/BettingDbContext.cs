using Cog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BettingService.Data;

public class BettingDbContext(DbContextOptions<BettingDbContext> options) : DbContext(options)
{
    public DbSet<Game> Games => Set<Game>();
    public DbSet<SportType> SportTypes => Set<SportType>();
    public DbSet<GamePeriod> GamePeriods => Set<GamePeriod>();
    public DbSet<LineSet> LineSets => Set<LineSet>();
    public DbSet<LineShade> LineShades => Set<LineShade>();
    public DbSet<Wager> Wagers => Set<Wager>();
    public DbSet<WagerItem> WagerItems => Set<WagerItem>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerBalance> CustomerBalances => Set<CustomerBalance>();
    public DbSet<CustomerLimits> CustomerLimits => Set<CustomerLimits>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BettingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
