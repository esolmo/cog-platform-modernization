using AlertsService.Data;
using AlertsService.Entities;
using AlertsService.Models;
using AlertsService.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AlertsService.Tests.Unit;

public class AlertDataServiceTests : IDisposable
{
    private readonly AlertsDbContext _db;
    private readonly AlertDataService _sut;

    public AlertDataServiceTests()
    {
        var options = new DbContextOptionsBuilder<AlertsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AlertsDbContext(options);
        _sut = new AlertDataService(_db, NullLogger<AlertDataService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetActionAlerts_ReturnsOnlyNonExpiredTickets()
    {
        var agentId = 10;
        _db.AlertTickets.AddRange(
            MakeTicket(agentId, wagerNumber: 1, expired: false),
            MakeTicket(agentId, wagerNumber: 2, expired: true));
        await _db.SaveChangesAsync();

        var filter = new AlertFilterRequest(agentId, 0, "", true, true, "", 0, false, null);
        var results = await _sut.GetActionAlertsAsync(filter);

        results.Should().HaveCount(1);
        results[0].WagerNumber.Should().Be(1);
    }

    [Fact]
    public async Task GetActionAlerts_FiltersWagerNumberGreaterThanLast()
    {
        var agentId = 10;
        _db.AlertTickets.AddRange(
            MakeTicket(agentId, wagerNumber: 100, expired: false),
            MakeTicket(agentId, wagerNumber: 200, expired: false),
            MakeTicket(agentId, wagerNumber: 300, expired: false));
        await _db.SaveChangesAsync();

        var filter = new AlertFilterRequest(agentId, 150, "", true, true, "", 0, false, null);
        var results = await _sut.GetActionAlertsAsync(filter);

        results.Should().HaveCount(2);
        results.Select(r => r.WagerNumber).Should().BeEquivalentTo([200, 300]);
    }

    [Fact]
    public async Task GetActionAlerts_FiltersOutTicketsBelowMinimumAmount()
    {
        var agentId = 10;
        _db.AlertTickets.AddRange(
            MakeTicket(agentId, wagerNumber: 1, expired: false, amount: 50),
            MakeTicket(agentId, wagerNumber: 2, expired: false, amount: 200),
            MakeTicket(agentId, wagerNumber: 3, expired: false, amount: 500));
        await _db.SaveChangesAsync();

        var filter = new AlertFilterRequest(agentId, 0, "", true, true, "", 100, false, null);
        var results = await _sut.GetActionAlertsAsync(filter);

        results.Should().HaveCount(2);
        results.All(r => r.Amount >= 100).Should().BeTrue();
    }

    [Fact]
    public async Task GetActionAlerts_ExcludesSharpActionWhenNotRequested()
    {
        var agentId = 10;
        _db.AlertTickets.AddRange(
            MakeTicket(agentId, 1, expired: false, isSharp: true),
            MakeTicket(agentId, 2, expired: false, isSharp: false));
        await _db.SaveChangesAsync();

        var filter = new AlertFilterRequest(agentId, 0, "", ShowSharp: false, ShowSquare: true, "", 0, false, null);
        var results = await _sut.GetActionAlertsAsync(filter);

        results.Should().HaveCount(1);
        results[0].IsSharpAction.Should().BeFalse();
    }

    [Fact]
    public async Task GetActionAlerts_ReturnsTicketsForRequestingAgentOnly()
    {
        _db.AlertTickets.AddRange(
            MakeTicket(agentId: 1, wagerNumber: 1, expired: false),
            MakeTicket(agentId: 2, wagerNumber: 2, expired: false),
            MakeTicket(agentId: 1, wagerNumber: 3, expired: false));
        await _db.SaveChangesAsync();

        var filter = new AlertFilterRequest(1, 0, "", true, true, "", 0, false, null);
        var results = await _sut.GetActionAlertsAsync(filter);

        results.Should().HaveCount(2);
        results.All(r => r.Id > 0).Should().BeTrue();
    }

    [Fact]
    public async Task GetVipSettings_ReturnsEmptyWhenNoSettingsExist()
    {
        var result = await _sut.GetVipSettingsAsync(999);

        result.Customers.Should().BeEmpty();
        result.Email.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateVipSettings_CreatesNewSettingsWhenNoneExist()
    {
        var request = new UpdateVipRequest(
            AgentId: 5,
            Customers: [new CustomerRefDto(1, "CUST01"), new CustomerRefDto(2, "CUST02")],
            Email: "agent@example.com");

        await _sut.UpdateVipSettingsAsync(request);

        var settings = await _db.AgentVipSettings
            .Include(v => v.VipCustomers)
            .FirstAsync(v => v.AgentId == 5);

        settings.NotificationEmail.Should().Be("agent@example.com");
        settings.VipCustomers.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateVipSettings_ReplacesExistingVipList()
    {
        _db.AgentVipSettings.Add(new AgentVipSettings
        {
            AgentId = 7,
            NotificationEmail = "old@example.com",
            VipCustomers = [new AgentVipCustomer { CustomerId = 99, CustomerLoginName = "OLDCUST" }]
        });
        await _db.SaveChangesAsync();

        var request = new UpdateVipRequest(
            AgentId: 7,
            Customers: [new CustomerRefDto(1, "NEWCUST")],
            Email: "new@example.com");

        await _sut.UpdateVipSettingsAsync(request);

        var settings = await _db.AgentVipSettings
            .Include(v => v.VipCustomers)
            .FirstAsync(v => v.AgentId == 7);

        settings.NotificationEmail.Should().Be("new@example.com");
        settings.VipCustomers.Should().HaveCount(1);
        settings.VipCustomers.First().CustomerLoginName.Should().Be("NEWCUST");
    }

    [Fact]
    public async Task GetAgentEmail_ReturnsEmptyStringWhenNotConfigured()
    {
        var email = await _sut.GetAgentEmailAsync(404);
        email.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCustomers_ReturnsVipCustomersForAgent()
    {
        _db.AgentVipSettings.Add(new AgentVipSettings
        {
            AgentId = 20,
            NotificationEmail = "agent@example.com",
            VipCustomers =
            [
                new AgentVipCustomer { CustomerId = 1, CustomerLoginName = "ALPHACUST" },
                new AgentVipCustomer { CustomerId = 2, CustomerLoginName = "BETACUST" }
            ]
        });
        await _db.SaveChangesAsync();

        var customers = await _sut.GetCustomersAsync(20);

        customers.Should().HaveCount(2);
        customers.Select(c => c.LoginName).Should().BeEquivalentTo(["ALPHACUST", "BETACUST"]);
    }

    [Fact]
    public async Task GetCustomers_ReturnsEmptyWhenAgentHasNoVipSettings()
    {
        var customers = await _sut.GetCustomersAsync(999);
        customers.Should().BeEmpty();
    }

    private static AlertTicket MakeTicket(
        int agentId,
        int wagerNumber,
        bool expired,
        decimal amount = 100,
        bool isSharp = false,
        AlertType alertType = AlertType.Straight) =>
        new()
        {
            AgentId = agentId,
            WagerNumber = wagerNumber,
            CustomerId = 1,
            CustomerLoginName = "TESTCUST",
            InetWagerNumber = $"W{wagerNumber}",
            WagerType = WagerType.Straight,
            AlertType = alertType,
            Amount = amount,
            Description = "Test wager",
            IsSharpAction = isSharp,
            InsertedAt = DateTime.UtcNow,
            ExpiresAt = expired ? DateTime.UtcNow.AddHours(-1) : DateTime.UtcNow.AddHours(24)
        };
}
