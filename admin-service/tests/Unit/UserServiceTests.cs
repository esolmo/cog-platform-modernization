using AdminService.Data;
using AdminService.Entities;
using AdminService.Models;
using AdminService.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AdminService.Tests.Unit;

public class UserServiceTests : IDisposable
{
    private readonly AdminDbContext _db;
    private readonly UserService _sut;
    private readonly Mock<IAuditService> _auditMock = new();
    private readonly Mock<IAuthProvisioningService> _authProvisioningMock = new();

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<AdminDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AdminDbContext(options);
        _sut = new UserService(_db, _auditMock.Object, _authProvisioningMock.Object, NullLogger<UserService>.Instance);

        // Seed roles for tests
        _db.Roles.AddRange(
            new Role { Id = 1, Name = "SuperAdmin", Description = "Full access", IsActive = true, IsSystemRole = true },
            new Role { Id = 2, Name = "Admin", Description = "Admin access", IsActive = true, IsSystemRole = true });
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CreateUser_WithValidRequest_ReturnsSuccessWithUserDto()
    {
        var request = new CreateUserRequest(
            Username: "jsmith",
            Password: "Password123!",
            Email: "jsmith@example.com",
            FirstName: "John",
            LastName: "Smith",
            MaxAccessLevel: "Admin",
            RoleIds: [1]);

        var result = await _sut.CreateUserAsync(request, createdByUserId: 1);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Username.Should().Be("jsmith");
        result.Value.Email.Should().Be("jsmith@example.com");
        result.Value.Roles.Should().Contain("SuperAdmin");
    }

    [Fact]
    public async Task CreateUser_WithDuplicateUsername_ReturnsFailure()
    {
        _db.Users.Add(new ApplicationUser { Username = "existing", Email = "e@example.com", PasswordHash = "x" });
        await _db.SaveChangesAsync();

        var request = new CreateUserRequest("existing", "pass", "new@e.com", "A", "B", "", []);

        var result = await _sut.CreateUserAsync(request, createdByUserId: 1);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("USERNAME_TAKEN");
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_ReturnsFailure()
    {
        _db.Users.Add(new ApplicationUser { Username = "unique", Email = "taken@example.com", PasswordHash = "x" });
        await _db.SaveChangesAsync();

        var request = new CreateUserRequest("newuser", "pass", "taken@example.com", "A", "B", "", []);

        var result = await _sut.CreateUserAsync(request, createdByUserId: 1);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("EMAIL_TAKEN");
    }

    [Fact]
    public async Task GetUsers_ReturnsPaginatedResults()
    {
        for (var i = 1; i <= 5; i++)
            _db.Users.Add(new ApplicationUser { Username = $"user{i}", Email = $"u{i}@ex.com", PasswordHash = "x" });
        await _db.SaveChangesAsync();

        var result = await _sut.GetUsersAsync(page: 1, pageSize: 3, search: null);

        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(2);
        result.HasNext.Should().BeTrue();
    }

    [Fact]
    public async Task GetUsers_WithSearchTerm_FiltersResults()
    {
        _db.Users.AddRange(
            new ApplicationUser { Username = "jdoe", Email = "jdoe@ex.com", FirstName = "John", LastName = "Doe", PasswordHash = "x" },
            new ApplicationUser { Username = "asmith", Email = "asmith@ex.com", FirstName = "Alice", LastName = "Smith", PasswordHash = "x" });
        await _db.SaveChangesAsync();

        var result = await _sut.GetUsersAsync(page: 1, pageSize: 10, search: "john");

        result.Items.Should().HaveCount(1);
        result.Items[0].Username.Should().Be("jdoe");
    }

    [Fact]
    public async Task UpdateUser_ChangesEmailAndActiveStatus()
    {
        var user = new ApplicationUser { Username = "target", Email = "old@ex.com", PasswordHash = "x", IsActive = true };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var request = new UpdateUserRequest("new@ex.com", "New", "Name", "Admin", IsActive: false, []);
        var result = await _sut.UpdateUserAsync(user.Id, request, updatedByUserId: 1);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Email.Should().Be("new@ex.com");
        result.Value.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteUser_SoftDeletesUser()
    {
        var user = new ApplicationUser { Username = "todelete", Email = "d@ex.com", PasswordHash = "x", IsActive = true };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteUserAsync(user.Id, deletedByUserId: 1);

        result.IsSuccess.Should().BeTrue();
        var dbUser = await _db.Users.FindAsync(user.Id);
        dbUser!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteUser_WhenNotFound_ReturnsFailure()
    {
        var result = await _sut.DeleteUserAsync(99999, deletedByUserId: 1);
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task ChangePassword_WithCorrectCurrentPassword_Succeeds()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("OldPass123!");
        var user = new ApplicationUser { Username = "chpwd", Email = "c@ex.com", PasswordHash = hash };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var result = await _sut.ChangePasswordAsync(user.Id, new ChangePasswordRequest("OldPass123!", "NewPass456!"));

        result.IsSuccess.Should().BeTrue();
        var updated = await _db.Users.FindAsync(user.Id);
        BCrypt.Net.BCrypt.Verify("NewPass456!", updated!.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_ReturnsFailure()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("CorrectPass!");
        var user = new ApplicationUser { Username = "wpwd", Email = "w@ex.com", PasswordHash = hash };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var result = await _sut.ChangePasswordAsync(user.Id, new ChangePasswordRequest("WrongPass!", "NewPass!"));

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_PASSWORD");
    }
}
