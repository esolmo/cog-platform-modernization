using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ReportsService.Controllers;
using ReportsService.Models;
using ReportsService.Services;
using Xunit;

namespace ReportsService.Tests.Unit;

public class ReportsControllerTests
{
    private readonly Mock<IReportsService> _serviceMock;
    private readonly ReportsController _controller;

    public ReportsControllerTests()
    {
        _serviceMock = new Mock<IReportsService>();
        _controller = new ReportsController(_serviceMock.Object);
    }

    [Fact]
    public async Task GetWagerActivity_WithValidLoginId_ReturnsOk()
    {
        var expectedRows = new List<WagerActivityRow>
        {
            new("DOC-001", DateTime.Today, "Straight", 50m, "Pick 3 ticket", 0),
            new("DOC-002", DateTime.Today, "Parlay", 100m, "3-team parlay", 1)
        };

        _serviceMock.Setup(s => s.GetWagerActivityAsync(
                It.Is<WagerActivityRequest>(r => r.LoginId == "agent1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRows);

        var result = await _controller.GetWagerActivity("agent1", DateTime.Today.AddDays(-7), DateTime.Today, default);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var rows = ok.Value.Should().BeAssignableTo<List<WagerActivityRow>>().Subject;
        rows.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetWagerActivity_WithEmptyLoginId_ReturnsBadRequest()
    {
        var result = await _controller.GetWagerActivity("", DateTime.Today, DateTime.Today, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetChangedTransactions_WithAgentScope_ReturnsOk()
    {
        var expectedRows = new List<ChangedTransactionRow>
        {
            new(1001, "player1", DateTime.Today, "Adjustment", -25m, "Manual credit", "REF-001"),
            new(1002, "player2", DateTime.Today, "Adjustment", 50m, "Deposit correction", "REF-002")
        };

        _serviceMock.Setup(s => s.GetChangedTransactionsAsync(
                It.Is<ChangedTransactionsRequest>(r => r.IdAgent == 5),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRows);

        var result = await _controller.GetChangedTransactions(5, 0, null, null, false, default);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var rows = ok.Value.Should().BeAssignableTo<List<ChangedTransactionRow>>().Subject;
        rows.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchAgents_ReturnsAgentList()
    {
        var expectedAgents = new List<AgentRow>
        {
            new(1, "agentA", "Agent Alpha", "agentA@cog.local", true),
            new(2, "agentB", "Agent Beta", "agentB@cog.local", true)
        };

        _serviceMock.Setup(s => s.SearchAgentsAsync(
                It.Is<AgentSearchRequest>(r => r.AgentId == 1 && r.Search == "ag"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAgents);

        var result = await _controller.SearchAgents(1, "ag", default);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var agents = ok.Value.Should().BeAssignableTo<List<AgentRow>>().Subject;
        agents.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchCustomers_ReturnsCustomerList()
    {
        var expectedCustomers = new List<CustomerRow>
        {
            new(101, "cust1", "Customer One", 5, 250m),
            new(102, "cust2", "Customer Two", 5, 0m)
        };

        _serviceMock.Setup(s => s.SearchCustomersAsync(
                It.IsAny<AgentSearchRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCustomers);

        var result = await _controller.SearchCustomers(5, "cust", default);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var customers = ok.Value.Should().BeAssignableTo<List<CustomerRow>>().Subject;
        customers.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPackageTracker_ReturnsPackageRows()
    {
        var expectedPackages = new List<PackageTrackerRow>
        {
            new(5001, DateTime.Today, 200m, "Delivered", "Agent X", "PKG-REF-1", "FedEx")
        };

        _serviceMock.Setup(s => s.GetPackageTrackerAsync(
                It.IsAny<PackageTrackerRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPackages);

        var result = await _controller.GetPackageTracker(0, null, null, 0, default);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var packages = ok.Value.Should().BeAssignableTo<List<PackageTrackerRow>>().Subject;
        packages.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetWagerActivity_WhenServiceReturnsEmpty_ReturnsOkWithEmptyList()
    {
        _serviceMock.Setup(s => s.GetWagerActivityAsync(
                It.IsAny<WagerActivityRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WagerActivityRow>());

        var result = await _controller.GetWagerActivity("nobody", DateTime.Today, DateTime.Today, default);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var rows = ok.Value.Should().BeAssignableTo<List<WagerActivityRow>>().Subject;
        rows.Should().BeEmpty();
    }
}
