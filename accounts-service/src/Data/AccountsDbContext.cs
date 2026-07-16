using AccountsService.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccountsService.Data;

public class AccountsDbContext(DbContextOptions<AccountsDbContext> options) : DbContext(options)
{
    public DbSet<Customer>              Customers              => Set<Customer>();
    public DbSet<Agent>                 Agents                 => Set<Agent>();
    public DbSet<CustomerBalance>       CustomerBalances       => Set<CustomerBalance>();
    public DbSet<CustomerLimits>        CustomerLimits         => Set<CustomerLimits>();
    public DbSet<CustomerTransaction>   CustomerTransactions   => Set<CustomerTransaction>();
    public DbSet<AgentDistribution>     AgentDistributions     => Set<AgentDistribution>();
    public DbSet<CustomerPermissions>   CustomerPermissions    => Set<CustomerPermissions>();
    public DbSet<CustomerComment>       CustomerComments       => Set<CustomerComment>();
    public DbSet<CustomerFreePlay>      CustomerFreePlays      => Set<CustomerFreePlay>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AccountsDbContext).Assembly);
    }
}
