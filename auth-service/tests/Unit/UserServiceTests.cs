using Xunit;
using AuthService.Data;
using AuthService.Entities;
using AuthService.Models.Requests;
using AuthService.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuthService.Tests.Unit;

public class UserServiceTests : IDisposable
{
    private readonly AuthDbContext _db;
    private readonly UserService   _sut;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new AuthDbContext(options);
        _sut = new UserService(_db, NullLogger<UserService>.Instance);

        SeedRoles();
    }

    [Fact]
    public async Task CreateUser_WithValidRequest_ReturnsNewUser()
    {
        var request = new CreateUserRequest
        {
            LoginName      = "newagent",
            Password       = "NewPass123!",
            UserType       = UserType.Agent,
            DomainEntityId = 5,
            RoleIds        = [1],
            CreatedBy      = "admin"
        };

        var result = await _sut.CreateUserAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.LoginName.Should().Be("newagent");
        result.Value.UserType.Should().Be("Agent");
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateUser_WithDuplicateLoginName_ReturnsFailure()
    {
        await _sut.CreateUserAsync(
            new CreateUserRequest { LoginName = "dupe", Password = "TestPass123!", RoleIds = [] },
            CancellationToken.None);

        var result = await _sut.CreateUserAsync(
            new CreateUserRequest { LoginName = "dupe", Password = "AnotherPass123!", RoleIds = [] },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_LOGIN");
    }

    [Fact]
    public async Task GetUserById_WithExistingUser_ReturnsUser()
    {
        var created = await _sut.CreateUserAsync(
            new CreateUserRequest { LoginName = "findme", Password = "FindMe123!", RoleIds = [] },
            CancellationToken.None);

        var result = await _sut.GetUserByIdAsync(created.Value!.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.LoginName.Should().Be("findme");
    }

    [Fact]
    public async Task GetUserById_WithNonExistentId_ReturnsFailure()
    {
        var result = await _sut.GetUserByIdAsync(99999, CancellationToken.None);
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task AssignRole_WithValidIds_AddsRole()
    {
        var user = await _sut.CreateUserAsync(
            new CreateUserRequest { LoginName = "roletest", Password = "RoleTest123!", RoleIds = [] },
            CancellationToken.None);

        var result = await _sut.AssignRoleAsync(user.Value!.Id, 1, "admin", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var updated = await _sut.GetUserByIdAsync(user.Value.Id, CancellationToken.None);
        updated.Value!.Roles.Should().Contain(RoleNames.Agent);
    }

    [Fact]
    public async Task AssignRole_Idempotent_DoesNotDuplicate()
    {
        var user = await _sut.CreateUserAsync(
            new CreateUserRequest { LoginName = "idempotent", Password = "Idempotent123!", RoleIds = [1] },
            CancellationToken.None);

        await _sut.AssignRoleAsync(user.Value!.Id, 1, "admin", CancellationToken.None);

        var roles = await _db.UserRoles.Where(ur => ur.UserId == user.Value.Id).ToListAsync();
        roles.Should().HaveCount(1, "duplicate role assignment must be ignored");
    }

    [Fact]
    public async Task RemoveRole_WithAssignedRole_RemovesIt()
    {
        var user = await _sut.CreateUserAsync(
            new CreateUserRequest { LoginName = "removetest", Password = "Remove123!", RoleIds = [1] },
            CancellationToken.None);

        await _sut.RemoveRoleAsync(user.Value!.Id, 1, CancellationToken.None);

        var updated = await _sut.GetUserByIdAsync(user.Value.Id, CancellationToken.None);
        updated.Value!.Roles.Should().NotContain(RoleNames.Agent);
    }

    [Fact]
    public async Task DeactivateUser_SetsIsActiveToFalse()
    {
        var user = await _sut.CreateUserAsync(
            new CreateUserRequest { LoginName = "deactivate", Password = "Deact123!", RoleIds = [] },
            CancellationToken.None);

        await _sut.DeactivateUserAsync(user.Value!.Id, CancellationToken.None);

        var deactivated = await _db.Users.FindAsync(user.Value.Id);
        deactivated!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ChangePassword_WithCorrectCurrentPassword_Succeeds()
    {
        var user = await _sut.CreateUserAsync(
            new CreateUserRequest { LoginName = "pwdchange", Password = "OldPass123!", RoleIds = [] },
            CancellationToken.None);

        var result = await _sut.ChangePasswordAsync(
            user.Value!.Id,
            new ChangePasswordRequest { CurrentPassword = "OldPass123!", NewPassword = "NewPass456!" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var dbUser  = await _db.Users.FindAsync(user.Value.Id);
        var matches = BCrypt.Net.BCrypt.Verify("NewPass456!", dbUser!.PasswordHash);
        matches.Should().BeTrue();
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_ReturnsFailure()
    {
        var user = await _sut.CreateUserAsync(
            new CreateUserRequest { LoginName = "pwdwrong", Password = "OldPass123!", RoleIds = [] },
            CancellationToken.None);

        var result = await _sut.ChangePasswordAsync(
            user.Value!.Id,
            new ChangePasswordRequest { CurrentPassword = "WrongPass!", NewPassword = "NewPass456!" },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_PASSWORD");
    }

    private void SeedRoles()
    {
        _db.Roles.Add(new Role { Id = 1, Name = RoleNames.Agent,  Description = "Agent",  CreatedBy = "seed", CreatedAt = DateTime.UtcNow });
        _db.Roles.Add(new Role { Id = 2, Name = RoleNames.Admin,  Description = "Admin",  CreatedBy = "seed", CreatedAt = DateTime.UtcNow });
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();
}
