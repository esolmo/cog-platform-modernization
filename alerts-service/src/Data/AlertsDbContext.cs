using AlertsService.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertsService.Data;

public class AlertsDbContext(DbContextOptions<AlertsDbContext> options) : DbContext(options)
{
    public DbSet<AlertTicket> AlertTickets => Set<AlertTicket>();
    public DbSet<AlertAttribute> AlertAttributes => Set<AlertAttribute>();
    public DbSet<AlertDetail> AlertDetails => Set<AlertDetail>();
    public DbSet<AlertDetailAttribute> AlertDetailAttributes => Set<AlertDetailAttribute>();
    public DbSet<AgentVipSettings> AgentVipSettings => Set<AgentVipSettings>();
    public DbSet<AgentVipCustomer> AgentVipCustomers => Set<AgentVipCustomer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AlertTicket>(b =>
        {
            b.HasKey(t => t.Id);
            b.Property(t => t.CustomerLoginName).HasMaxLength(20);
            b.Property(t => t.InetWagerNumber).HasMaxLength(50);
            b.Property(t => t.Description).HasMaxLength(500);
            b.Property(t => t.Amount).HasColumnType("decimal(18,2)");
            b.HasIndex(t => t.AgentId);
            b.HasIndex(t => t.InsertedAt);
            b.HasIndex(t => t.ExpiresAt);
        });

        modelBuilder.Entity<AlertAttribute>(b =>
        {
            b.HasKey(a => a.Id);
            b.Property(a => a.Name).HasMaxLength(100);
            b.Property(a => a.Value).HasMaxLength(500);
            b.HasOne(a => a.AlertTicket)
             .WithMany(t => t.Attributes)
             .HasForeignKey(a => a.AlertTicketId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AlertDetail>(b =>
        {
            b.HasKey(d => d.Id);
            b.Property(d => d.Description).HasMaxLength(500);
            b.Property(d => d.SportKey).HasMaxLength(50);
            b.HasOne(d => d.AlertTicket)
             .WithMany(t => t.Details)
             .HasForeignKey(d => d.AlertTicketId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AlertDetailAttribute>(b =>
        {
            b.HasKey(a => a.Id);
            b.Property(a => a.Name).HasMaxLength(100);
            b.Property(a => a.Value).HasMaxLength(500);
            b.HasOne(a => a.AlertDetail)
             .WithMany(d => d.Attributes)
             .HasForeignKey(a => a.AlertDetailId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentVipSettings>(b =>
        {
            b.HasKey(v => v.Id);
            b.Property(v => v.NotificationEmail).HasMaxLength(200);
            b.HasIndex(v => v.AgentId).IsUnique();
        });

        modelBuilder.Entity<AgentVipCustomer>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.CustomerLoginName).HasMaxLength(20);
            b.HasOne(c => c.AgentVipSettings)
             .WithMany(v => v.VipCustomers)
             .HasForeignKey(c => c.AgentVipSettingsId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
