using AccountsService.Data;
using AccountsService.Entities;
using AccountsService.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace AccountsService.Tests.Unit;

public class AgentServiceTests : IDisposable
{
    private readonly AccountsDbContext _db;
    private readonly AgentService      _sut;

    public AgentServiceTests()
    {
        var options = new DbContextOptionsBuilder<AccountsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db  = new AccountsDbContext(options);
        _sut = new AgentService(_db, Substitute.For<IBettingProvisioningService>(), NullLogger<AgentService>.Instance);

        SeedAgentHierarchy();
    }

    // Hierarchy: Master (1) → Agent (2) → SubAgent (3)
    //                       → Agent (4)
    private void SeedAgentHierarchy()
    {
        _db.Agents.AddRange(
            new Agent { Id = 1, LoginName = "master01",  AgentType = AgentType.Master,   CreditLimitMax = 100_000m, CreatedBy = "seed" },
            new Agent { Id = 2, LoginName = "agent01",   AgentType = AgentType.Agent,    CreditLimitMax = 10_000m,  CreatedBy = "seed", ParentAgentId = 1 },
            new Agent { Id = 3, LoginName = "subagent01", AgentType = AgentType.SubAgent, CreditLimitMax = 5_000m,   CreatedBy = "seed", ParentAgentId = 2 },
            new Agent { Id = 4, LoginName = "agent02",   AgentType = AgentType.Agent,    CreditLimitMax = 8_000m,   CreatedBy = "seed", ParentAgentId = 1 }
        );
        _db.SaveChanges();
    }

    [Fact]
    public async Task GetAgentById_WithExistingAgent_ReturnsAgent()
    {
        var result = await _sut.GetAgentByIdAsync(1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.LoginName.Should().Be("master01");
        result.Value.AgentType.Should().Be("Master");
    }

    [Fact]
    public async Task GetAgentById_WithNonExistentAgent_ReturnsFailure()
    {
        var result = await _sut.GetAgentByIdAsync(99999, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task GetSubAgents_ReturnsDirectChildrenOnly()
    {
        var result = await _sut.GetSubAgentsAsync(1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(2, "master01 has agent01 and agent02 as direct children");
        result.Value.Select(a => a.LoginName).Should().BeEquivalentTo(["agent01", "agent02"]);
    }

    [Fact]
    public async Task GetSubAgents_ForLeafAgent_ReturnsEmpty()
    {
        var result = await _sut.GetSubAgentsAsync(3, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().BeEmpty("subagent01 has no children");
    }

    [Fact]
    public async Task GetHierarchy_ReturnsFullTree()
    {
        var result = await _sut.GetHierarchyAsync(1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var root = result.Value!;
        root.LoginName.Should().Be("master01");
        root.Children.Should().HaveCount(2);

        var agent01Node = root.Children.First(c => c.LoginName == "agent01");
        agent01Node.Children.Should().HaveCount(1);
        agent01Node.Children[0].LoginName.Should().Be("subagent01");
    }

    [Fact]
    public async Task GetHierarchy_LevelsAreCorrect()
    {
        var result = await _sut.GetHierarchyAsync(1, CancellationToken.None);

        result.Value!.Level.Should().Be(0);
        result.Value.Children[0].Level.Should().Be(1);
        result.Value.Children.First(c => c.LoginName == "agent01")
            .Children[0].Level.Should().Be(2);
    }

    [Fact]
    public async Task GetAllSubAgentIds_IncludesSelfAndAllDescendants()
    {
        var result = await _sut.GetAllSubAgentIdsAsync(1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().BeEquivalentTo([1, 2, 3, 4],
            "master01 + agent01 + subagent01 + agent02");
    }

    [Fact]
    public async Task GetAllSubAgentIds_ForMidLevelAgent_ExcludesParent()
    {
        var result = await _sut.GetAllSubAgentIdsAsync(2, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().BeEquivalentTo([2, 3],
            "agent01 and subagent01 only");
        result.Value.Should().NotContain(1, "master01 is the parent, not a descendant");
    }

    public void Dispose() => _db.Dispose();
}
