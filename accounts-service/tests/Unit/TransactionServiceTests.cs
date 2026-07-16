using AccountsService.Data;
using AccountsService.Entities;
using AccountsService.Models.Requests;
using AccountsService.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AccountsService.Tests.Unit;

public class TransactionServiceTests : IDisposable
{
    private readonly AccountsDbContext  _db;
    private readonly TransactionService _sut;
    private readonly Customer           _testCustomer;

    public TransactionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AccountsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            // InMemory does not support transactions; suppress the warning so
            // TransactionService.BeginTransactionAsync is effectively a no-op.
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db  = new AccountsDbContext(options);
        _sut = new TransactionService(_db, NullLogger<TransactionService>.Instance);

        _testCustomer = SeedCustomer();
    }

    private Customer SeedCustomer()
    {
        var agent = new Agent
        {
            Id = 1, LoginName = "agent01", AgentType = AgentType.Agent,
            CreditLimitMax = 10_000m, CreatedBy = "seed"
        };
        _db.Agents.Add(agent);

        var customer = new Customer
        {
            Id        = 1,
            LoginName = "player01",
            AgentId   = 1,
            IsActive  = true,
            CreatedBy = "seed",
            Balance = new CustomerBalance
            {
                CustomerId   = 1,
                CreditLimit  = 1000m,
                WagerLimit   = 500m,
                CurrentBalance = 0m
            },
            Limits = new CustomerLimits { CustomerId = 1 }
        };
        _db.Customers.Add(customer);
        _db.SaveChanges();
        return customer;
    }

    [Fact]
    public async Task CreateTransaction_Credit_IncreasesBalance()
    {
        var request = new CreateTransactionRequest
        {
            CustomerId = 1,
            Code       = TransactionCode.Credit,
            Type       = TransactionType.Cash,
            Amount     = 200m,
            EnteredBy  = "cashier"
        };

        var result = await _sut.CreateTransactionAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.BalanceBefore.Should().Be(0m);
        result.Value.BalanceAfter.Should().Be(200m);

        var balance = await _db.CustomerBalances.FindAsync(1);
        balance!.CurrentBalance.Should().Be(200m);
    }

    [Fact]
    public async Task CreateTransaction_Debit_DecreasesBalance()
    {
        // First credit the account
        await _sut.CreateTransactionAsync(new CreateTransactionRequest
        {
            CustomerId = 1, Code = TransactionCode.Credit,
            Type = TransactionType.Cash, Amount = 500m, EnteredBy = "cashier"
        }, CancellationToken.None);

        var result = await _sut.CreateTransactionAsync(new CreateTransactionRequest
        {
            CustomerId = 1, Code = TransactionCode.Debit,
            Type = TransactionType.Wire, Amount = 100m, EnteredBy = "cashier"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.BalanceAfter.Should().Be(400m);
    }

    [Fact]
    public async Task CreateTransaction_Debit_WithInsufficientCredit_ReturnsFailure()
    {
        // Balance is 0, CreditLimit is 1000 — available = 1000 - 0 - 0 = 1000
        // Requesting 1500 which exceeds available credit
        var result = await _sut.CreateTransactionAsync(new CreateTransactionRequest
        {
            CustomerId = 1, Code = TransactionCode.Debit,
            Type = TransactionType.Wire, Amount = 1500m, EnteredBy = "cashier"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INSUFFICIENT_CREDIT");
    }

    [Fact]
    public async Task CreateTransaction_ForNonExistentCustomer_ReturnsFailure()
    {
        var result = await _sut.CreateTransactionAsync(new CreateTransactionRequest
        {
            CustomerId = 99999, Code = TransactionCode.Credit,
            Type = TransactionType.Cash, Amount = 100m, EnteredBy = "cashier"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task CreateTransaction_ForInactiveCustomer_ReturnsFailure()
    {
        var customer = await _db.Customers.FindAsync(1);
        customer!.IsActive = false;
        await _db.SaveChangesAsync();

        var result = await _sut.CreateTransactionAsync(new CreateTransactionRequest
        {
            CustomerId = 1, Code = TransactionCode.Credit,
            Type = TransactionType.Cash, Amount = 100m, EnteredBy = "cashier"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("CUSTOMER_INACTIVE");
    }

    [Fact]
    public async Task GetTransactions_ReturnsPaginatedResults()
    {
        for (var i = 0; i < 5; i++)
        {
            await _sut.CreateTransactionAsync(new CreateTransactionRequest
            {
                CustomerId = 1, Code = TransactionCode.Credit,
                Type = TransactionType.Cash, Amount = 10m, EnteredBy = "cashier"
            }, CancellationToken.None);
        }

        var result = await _sut.GetTransactionsAsync(1, page: 1, pageSize: 3, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(3);
        result.Value.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task VerifyTransaction_SetsVerifiedFlag()
    {
        var created = await _sut.CreateTransactionAsync(new CreateTransactionRequest
        {
            CustomerId = 1, Code = TransactionCode.Credit,
            Type = TransactionType.Cash, Amount = 50m, EnteredBy = "cashier"
        }, CancellationToken.None);

        var result = await _sut.VerifyTransactionAsync(created.Value!.Id, "supervisor", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsVerified.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyTransaction_AlreadyVerified_ReturnsFailure()
    {
        var created = await _sut.CreateTransactionAsync(new CreateTransactionRequest
        {
            CustomerId = 1, Code = TransactionCode.Credit,
            Type = TransactionType.Cash, Amount = 50m, EnteredBy = "cashier"
        }, CancellationToken.None);

        await _sut.VerifyTransactionAsync(created.Value!.Id, "supervisor", CancellationToken.None);
        var result = await _sut.VerifyTransactionAsync(created.Value.Id, "supervisor", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ALREADY_VERIFIED");
    }

    public void Dispose() => _db.Dispose();
}
