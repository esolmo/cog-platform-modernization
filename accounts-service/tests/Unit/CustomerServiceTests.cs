using AccountsService.Data;
using AccountsService.Entities;
using AccountsService.Models.Requests;
using AccountsService.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace AccountsService.Tests.Unit;

public class CustomerServiceTests : IDisposable
{
    private readonly AccountsDbContext _db;
    private readonly CustomerService   _sut;
    private readonly Agent             _testAgent;

    public CustomerServiceTests()
    {
        var options = new DbContextOptionsBuilder<AccountsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db  = new AccountsDbContext(options);
        _sut = new CustomerService(_db, Substitute.For<IBettingProvisioningService>(), NullLogger<CustomerService>.Instance);

        _testAgent = new Agent
        {
            Id             = 1,
            LoginName      = "agent01",
            AgentType      = AgentType.Agent,
            CreditLimitMax = 10_000m,
            WagerLimitMax  = 5_000m,
            CreatedBy      = "seed"
        };
        _db.Agents.Add(_testAgent);
        _db.SaveChanges();
    }

    [Fact]
    public async Task CreateCustomer_WithValidRequest_ReturnsNewCustomer()
    {
        var request = BuildCreateRequest("player01", agentId: 1, creditLimit: 1000m);

        var result = await _sut.CreateCustomerAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.LoginName.Should().Be("player01");
        result.Value.AgentId.Should().Be(1);
        result.Value.Balance.CreditLimit.Should().Be(1000m);
    }

    [Fact]
    public async Task CreateCustomer_WithDuplicateLoginName_ReturnsFailure()
    {
        await _sut.CreateCustomerAsync(BuildCreateRequest("dupe", 1), CancellationToken.None);

        var result = await _sut.CreateCustomerAsync(BuildCreateRequest("dupe", 1), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_LOGIN");
    }

    [Fact]
    public async Task CreateCustomer_WithNonExistentAgent_ReturnsFailure()
    {
        var result = await _sut.CreateCustomerAsync(
            BuildCreateRequest("player02", agentId: 999), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("AGENT_NOT_FOUND");
    }

    [Fact]
    public async Task CreateCustomer_WithCreditLimitAboveAgentMax_ReturnsFailure()
    {
        var result = await _sut.CreateCustomerAsync(
            BuildCreateRequest("player03", agentId: 1, creditLimit: 99_999m),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("CREDIT_LIMIT_EXCEEDS_AGENT_MAX");
    }

    [Fact]
    public async Task GetCustomerById_WithExistingCustomer_ReturnsCustomer()
    {
        var created = await _sut.CreateCustomerAsync(
            BuildCreateRequest("findme", 1), CancellationToken.None);

        var result = await _sut.GetCustomerByIdAsync(created.Value!.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.LoginName.Should().Be("findme");
    }

    [Fact]
    public async Task GetCustomerById_WithNonExistentId_ReturnsFailure()
    {
        var result = await _sut.GetCustomerByIdAsync(99999, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task GetCustomerByLogin_WithExistingLogin_ReturnsCustomer()
    {
        await _sut.CreateCustomerAsync(BuildCreateRequest("logintest", 1), CancellationToken.None);

        var result = await _sut.GetCustomerByLoginAsync("logintest", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.LoginName.Should().Be("logintest");
    }

    [Fact]
    public async Task UpdateCreditLimits_WithValidLimits_UpdatesBalance()
    {
        var created = await _sut.CreateCustomerAsync(
            BuildCreateRequest("creditupd", 1, creditLimit: 500m), CancellationToken.None);

        var result = await _sut.UpdateCreditLimitsAsync(
            created.Value!.Id,
            new UpdateCreditLimitRequest
            {
                CreditLimit     = 2000m,
                WagerLimit      = 1000m,
                HardCreditLimit = 5000m,
                UpdatedBy       = "admin"
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Balance.CreditLimit.Should().Be(2000m);
    }

    [Fact]
    public async Task UpdateCreditLimits_ExceedingAgentMax_ReturnsFailure()
    {
        var created = await _sut.CreateCustomerAsync(
            BuildCreateRequest("creditfail", 1, creditLimit: 100m), CancellationToken.None);

        var result = await _sut.UpdateCreditLimitsAsync(
            created.Value!.Id,
            new UpdateCreditLimitRequest
            {
                CreditLimit = 50_000m,
                UpdatedBy   = "admin"
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("CREDIT_LIMIT_EXCEEDS_AGENT_MAX");
    }

    [Fact]
    public async Task SuspendCustomer_SetsStatusToSuspended()
    {
        var created = await _sut.CreateCustomerAsync(
            BuildCreateRequest("sustest", 1), CancellationToken.None);

        var result = await _sut.SuspendCustomerAsync(created.Value!.Id, "admin", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var customer = await _db.Customers.FindAsync(created.Value.Id);
        customer!.IsActive.Should().BeFalse();
        customer.Status.Should().Be(CustomerStatus.Suspended);
    }

    [Fact]
    public async Task ActivateCustomer_SetsStatusToActive()
    {
        var created = await _sut.CreateCustomerAsync(
            BuildCreateRequest("acttest", 1), CancellationToken.None);
        await _sut.SuspendCustomerAsync(created.Value!.Id, "admin", CancellationToken.None);

        var result = await _sut.ActivateCustomerAsync(created.Value.Id, "admin", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var customer = await _db.Customers.FindAsync(created.Value.Id);
        customer!.IsActive.Should().BeTrue();
        customer.Status.Should().Be(CustomerStatus.Active);
    }

    [Fact]
    public async Task GetCustomersByAgent_ReturnsPaginatedResults()
    {
        for (var i = 1; i <= 5; i++)
        {
            await _sut.CreateCustomerAsync(
                BuildCreateRequest($"bulk{i:D2}", 1), CancellationToken.None);
        }

        var result = await _sut.GetCustomersByAgentAsync(1, page: 1, pageSize: 3, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(3);
        result.Value.TotalCount.Should().Be(5);
        result.Value.TotalPages.Should().Be(2);
        result.Value.HasNext.Should().BeTrue();
        result.Value.HasPrevious.Should().BeFalse();
    }

    private static CreateCustomerRequest BuildCreateRequest(
        string loginName, int agentId, decimal creditLimit = 500m) =>
        new()
        {
            LoginName    = loginName,
            AgentId      = agentId,
            CreditLimit  = creditLimit,
            WagerLimit   = creditLimit / 2,
            CreatedBy    = "test"
        };

    public void Dispose() => _db.Dispose();
}
