using AdminService.Data;
using AdminService.Entities;
using AdminService.Models;
using AdminService.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AdminService.Tests.Unit;

public class RoleServiceTests : IDisposable
{
    private readonly AdminDbContext _db;
    private readonly RoleService _sut;
    private readonly Mock<IAuditService> _auditMock = new();

    public RoleServiceTests()
    {
        var options = new DbContextOptionsBuilder<AdminDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AdminDbContext(options);
        _sut = new RoleService(_db, _auditMock.Object, NullLogger<RoleService>.Instance);

        _db.Permissions.AddRange(
            new Permission { Id = 1, Name = "wagers.view", Category = "Wagers", Description = "View wagers" },
            new Permission { Id = 2, Name = "wagers.create", Category = "Wagers", Description = "Create wagers" });
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CreateRole_WithValidRequest_ReturnsRoleWithPermissions()
    {
        var request = new CreateRoleRequest("Trader", "Manages trading lines", [1, 2]);

        var result = await _sut.CreateRoleAsync(request, createdByUserId: 1);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Trader");
        result.Value.Permissions.Should().HaveCount(2);
        result.Value.Permissions.Select(p => p.Name).Should().BeEquivalentTo(["wagers.view", "wagers.create"]);
    }

    [Fact]
    public async Task CreateRole_WithDuplicateName_ReturnsFailure()
    {
        _db.Roles.Add(new Role { Name = "Duplicated", Description = "x", IsActive = true });
        await _db.SaveChangesAsync();

        var result = await _sut.CreateRoleAsync(new CreateRoleRequest("Duplicated", "desc", []), createdByUserId: 1);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ROLE_EXISTS");
    }

    [Fact]
    public async Task UpdateRole_ReplacesPermissions()
    {
        var role = new Role { Name = "Modifiable", Description = "original", IsActive = true, IsSystemRole = false };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();
        _db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = 1 });
        await _db.SaveChangesAsync();

        var request = new UpdateRoleRequest("updated desc", IsActive: true, PermissionIds: [2]);
        var result = await _sut.UpdateRoleAsync(role.Id, request, updatedByUserId: 1);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Permissions.Should().HaveCount(1);
        result.Value.Permissions[0].Name.Should().Be("wagers.create");
    }

    [Fact]
    public async Task DeleteRole_SystemRole_ReturnsFailure()
    {
        var role = new Role { Name = "Protected", Description = "x", IsActive = true, IsSystemRole = true };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteRoleAsync(role.Id, deletedByUserId: 1);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SYSTEM_ROLE_PROTECTED");
    }

    [Fact]
    public async Task DeleteRole_NonSystemRole_Succeeds()
    {
        var role = new Role { Name = "Deletable", Description = "x", IsActive = true, IsSystemRole = false };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteRoleAsync(role.Id, deletedByUserId: 1);

        result.IsSuccess.Should().BeTrue();
        _db.Roles.Should().NotContain(r => r.Id == role.Id);
    }

    [Fact]
    public async Task GetPermissions_ReturnsAllSeededPermissions()
    {
        var permissions = await _sut.GetPermissionsAsync();
        permissions.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateRole_DeactivatingSystemRole_ReturnsFailure()
    {
        var role = new Role { Name = "SysRole", Description = "x", IsActive = true, IsSystemRole = true };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        var request = new UpdateRoleRequest("desc", IsActive: false, []);
        var result = await _sut.UpdateRoleAsync(role.Id, request, updatedByUserId: 1);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SYSTEM_ROLE_PROTECTED");
    }
}
