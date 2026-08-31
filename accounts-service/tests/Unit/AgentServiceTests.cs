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

public class AgentServiceTests : IDisposable
{
    private readonly AccountsDbContext _db;
    private readonly IBettingProvisioningService _bettingProvisioning;
    private readonly IAuthProvisioningService    _authProvisioning;
    private readonly AgentService      _sut;

    public AgentServiceTests()
    {
        var options = new DbContextOptionsBuilder<AccountsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db  = new AccountsDbContext(options);
        _bettingProvisioning = Substitute.For<IBettingProvisioningService>();
        _authProvisioning    = Substitute.For<IAuthProvisioningService>();
        _sut = new AgentService(_db, _bettingProvisioning, _authProvisioning, NullLogger<AgentService>.Instance);

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

    // ─── CreateAgentAsync ───────────────────────────────────────────────────────

    private static CreateAgentRequest ValidCreateRequest(
        string loginName = "newagent01",
        AgentType agentType = AgentType.Agent,
        int? parentAgentId = null,
        decimal creditLimitMax = 5_000m) => new()
    {
        LoginName      = loginName,
        Name           = "New Agent",
        ParentAgentId  = parentAgentId,
        AgentType      = agentType,
        CreditLimitMax = creditLimitMax,
        WagerLimitMax  = 1_000m,
        CommissionType = CommissionType.WeeklyProfit,
        CommissionRate = 50m,
        CreatedBy      = "test"
    };

    [Fact]
    public async Task CreateAgent_ValidRequest_PersistsAgentAndReturnsSuccess()
    {
        var result = await _sut.CreateAgentAsync(ValidCreateRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.LoginName.Should().Be("newagent01");
        result.Value.AgentType.Should().Be("Agent");
        (await _db.Agents.AnyAsync(a => a.LoginName == "newagent01")).Should().BeTrue();
    }

    [Fact]
    public async Task CreateAgent_ValidRequest_ReturnsNonEmptyTemporaryPassword()
    {
        var result = await _sut.CreateAgentAsync(ValidCreateRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TemporaryPassword.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CreateAgent_ValidRequest_TemporaryPasswordMeetsAuthServicePasswordPolicy()
    {
        // auth-service's CreateUserRequestValidator requires: length >= 8, >=1 uppercase, >=1 digit.
        var result = await _sut.CreateAgentAsync(ValidCreateRequest(), CancellationToken.None);

        var password = result.Value!.TemporaryPassword!;
        password.Length.Should().BeGreaterThanOrEqualTo(8);
        password.Should().MatchRegex("[A-Z]");
        password.Should().MatchRegex("[0-9]");
    }

    [Fact]
    public async Task CreateAgent_TwoAgents_GeneratesDifferentTemporaryPasswords()
    {
        var first  = await _sut.CreateAgentAsync(ValidCreateRequest("agentA"), CancellationToken.None);
        var second = await _sut.CreateAgentAsync(ValidCreateRequest("agentB"), CancellationToken.None);

        first.Value!.TemporaryPassword.Should().NotBe(second.Value!.TemporaryPassword);
    }

    [Fact]
    public async Task CreateAgent_ValidRequest_CallsBettingProvisioningWithNewAgentDetails()
    {
        await _sut.CreateAgentAsync(ValidCreateRequest("newagent01", AgentType.Agent), CancellationToken.None);

        await _bettingProvisioning.Received(1).ProvisionAgentAsync(
            "newagent01", "New Agent", null, (int)AgentType.Agent, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAgent_ValidRequest_CallsAuthProvisioningWithNewAgentIdAndGeneratedPassword()
    {
        var result = await _sut.CreateAgentAsync(ValidCreateRequest("newagent01"), CancellationToken.None);
        var newAgentId = result.Value!.Id;

        await _authProvisioning.Received(1).ProvisionAgentAsync(
            "newagent01", result.Value.TemporaryPassword!, newAgentId, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(AgentType.Master, "MasterAgent")]
    [InlineData(AgentType.Agent, "Agent")]
    [InlineData(AgentType.SubAgent, "SubAgent")]
    public async Task CreateAgent_MapsAgentTypeToMatchingAuthServiceRoleName(AgentType agentType, string expectedRole)
    {
        await _sut.CreateAgentAsync(ValidCreateRequest($"agent_{agentType}", agentType), CancellationToken.None);

        await _authProvisioning.Received(1).ProvisionAgentAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), expectedRole, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAgent_WithValidParent_SetsParentAgentIdAndPassesItToBettingProvisioning()
    {
        var result = await _sut.CreateAgentAsync(
            ValidCreateRequest("subagentNew", AgentType.SubAgent, parentAgentId: 2, creditLimitMax: 1_000m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ParentAgentId.Should().Be(2);
        result.Value.ParentLoginName.Should().Be("agent01");

        await _bettingProvisioning.Received(1).ProvisionAgentAsync(
            "subagentNew", Arg.Any<string?>(), 2, (int)AgentType.SubAgent, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAgent_DuplicateLoginName_ReturnsFailureAndSkipsProvisioning()
    {
        var result = await _sut.CreateAgentAsync(ValidCreateRequest("master01"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_LOGIN");
        await _bettingProvisioning.DidNotReceive().ProvisionAgentAsync(
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _authProvisioning.DidNotReceive().ProvisionAgentAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAgent_NonExistentParent_ReturnsFailureAndSkipsProvisioning()
    {
        var result = await _sut.CreateAgentAsync(
            ValidCreateRequest("orphan", parentAgentId: 99999), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PARENT_NOT_FOUND");
        await _authProvisioning.DidNotReceive().ProvisionAgentAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAgent_CreditLimitExceedsParentMaximum_ReturnsFailureAndSkipsProvisioning()
    {
        // agent01 (Id=2) has CreditLimitMax = 10_000m
        var result = await _sut.CreateAgentAsync(
            ValidCreateRequest("overLimit", parentAgentId: 2, creditLimitMax: 50_000m), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("CREDIT_LIMIT_EXCEEDS_PARENT");
        await _authProvisioning.DidNotReceive().ProvisionAgentAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAgent_AuthProvisioningThrows_StillReturnsSuccessWithAgentPersisted()
    {
        // Provisioning failures must be non-fatal — the agent record is already committed
        // by the time provisioning runs, so a throwing provisioning call (even one that
        // doesn't follow the "swallow your own exceptions" convention) must not turn an
        // already-successful agent creation into a 500 response.
        _authProvisioning
            .ProvisionAgentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new HttpRequestException("auth-service unreachable")));

        var result = await _sut.CreateAgentAsync(ValidCreateRequest("resilientAgent"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _db.Agents.AnyAsync(a => a.LoginName == "resilientAgent")).Should().BeTrue();
    }

    [Fact]
    public async Task CreateAgent_BettingProvisioningThrows_StillReturnsSuccessAndStillCallsAuthProvisioning()
    {
        _bettingProvisioning
            .ProvisionAgentAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new HttpRequestException("betting-service unreachable")));

        var result = await _sut.CreateAgentAsync(ValidCreateRequest("resilientAgent2"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _authProvisioning.Received(1).ProvisionAgentAsync(
            "resilientAgent2", Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    public void Dispose() => _db.Dispose();
}
