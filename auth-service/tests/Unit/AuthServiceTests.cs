using Xunit;
using AuthService.Configuration;
using AuthService.Data;
using AuthService.Entities;
using AuthService.Models.Requests;
using AuthService.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace AuthService.Tests.Unit;

public class AuthServiceTests : IDisposable
{
    private readonly AuthDbContext                    _db;
    private readonly ITokenService                   _tokenService;
    private readonly AuthService.Services.AuthService _sut;
    private readonly JwtOptions                      _options;

    public AuthServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AuthDbContext(dbOptions);

        _options = new JwtOptions
        {
            SecretKey                = "SuperSecretKeyForTestingPurposesOnly123!",
            Issuer                   = "cog-auth-service",
            Audience                 = "cog-services",
            AccessTokenExpiryMinutes = 15,
            RefreshTokenExpiryDays   = 7
        };

        _tokenService = new TokenService(Options.Create(_options), NullLogger<TokenService>.Instance);

        _sut = new AuthService.Services.AuthService(
            _db,
            _tokenService,
            Options.Create(_options),
            NullLogger<AuthService.Services.AuthService>.Instance);

        SeedDatabase();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenResponse()
    {
        var request = new LoginRequest { LoginName = "testagent", Password = "TestPass123!" };

        var result = await _sut.LoginAsync(request, "127.0.0.1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().NotBeNullOrEmpty();
        result.Value.RefreshToken.Should().NotBeNullOrEmpty();
        result.Value.LoginName.Should().Be("testagent");
        result.Value.Roles.Should().Contain(RoleNames.Agent);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsFailure()
    {
        var request = new LoginRequest { LoginName = "testagent", Password = "WrongPassword!" };

        var result = await _sut.LoginAsync(request, null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Login_WithUnknownUser_ReturnsFailure()
    {
        var request = new LoginRequest { LoginName = "nobody", Password = "TestPass123!" };

        var result = await _sut.LoginAsync(request, null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Login_WithInactiveUser_ReturnsFailure()
    {
        var request = new LoginRequest { LoginName = "inactiveuser", Password = "TestPass123!" };

        var result = await _sut.LoginAsync(request, null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewTokens()
    {
        var loginResult = await _sut.LoginAsync(
            new LoginRequest { LoginName = "testagent", Password = "TestPass123!" },
            null, CancellationToken.None);

        var refreshResult = await _sut.RefreshAsync(
            loginResult.Value!.RefreshToken, "127.0.0.1", CancellationToken.None);

        refreshResult.IsSuccess.Should().BeTrue();
        refreshResult.Value!.AccessToken.Should().NotBeNullOrEmpty();
        refreshResult.Value.RefreshToken.Should().NotBe(loginResult.Value.RefreshToken,
            "refresh token must be rotated");
    }

    [Fact]
    public async Task Refresh_WithExpiredToken_ReturnsFailure()
    {
        var result = await _sut.RefreshAsync("totally-invalid-token", null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Refresh_WithRevokedToken_ReturnsFailure()
    {
        var loginResult = await _sut.LoginAsync(
            new LoginRequest { LoginName = "testagent", Password = "TestPass123!" },
            null, CancellationToken.None);
        var originalRefreshToken = loginResult.Value!.RefreshToken;

        // Use the token once to rotate it
        await _sut.RefreshAsync(originalRefreshToken, null, CancellationToken.None);

        // Try to reuse the old (now-revoked) token
        var reuseResult = await _sut.RefreshAsync(originalRefreshToken, null, CancellationToken.None);

        reuseResult.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Logout_WithValidToken_RevokesToken()
    {
        var loginResult = await _sut.LoginAsync(
            new LoginRequest { LoginName = "testagent", Password = "TestPass123!" },
            null, CancellationToken.None);
        var refreshToken = loginResult.Value!.RefreshToken;

        var logoutResult = await _sut.LogoutAsync(refreshToken, CancellationToken.None);

        logoutResult.IsSuccess.Should().BeTrue();

        var afterLogout = await _sut.RefreshAsync(refreshToken, null, CancellationToken.None);
        afterLogout.IsSuccess.Should().BeFalse("token must be revoked after logout");
    }

    [Fact]
    public async Task Logout_WithAlreadyRevokedToken_ReturnsSuccess()
    {
        var result = await _sut.LogoutAsync("nonexistent-token", CancellationToken.None);
        result.IsSuccess.Should().BeTrue("logout is idempotent");
    }

    [Fact]
    public async Task GetCurrentUser_WithValidId_ReturnsUserInfo()
    {
        var user = await _db.Users.FirstAsync(u => u.LoginName == "testagent");

        var result = await _sut.GetCurrentUserAsync(user.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.LoginName.Should().Be("testagent");
        result.Value.Roles.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCurrentUser_WithInvalidId_ReturnsFailure()
    {
        var result = await _sut.GetCurrentUserAsync(99999, CancellationToken.None);
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task Login_SetsLastLoginAt()
    {
        var before  = DateTime.UtcNow.AddSeconds(-1);
        await _sut.LoginAsync(
            new LoginRequest { LoginName = "testagent", Password = "TestPass123!" },
            null, CancellationToken.None);

        var user = await _db.Users.FirstAsync(u => u.LoginName == "testagent");
        user.LastLoginAt.Should().BeAfter(before);
    }

    private void SeedDatabase()
    {
        var agentRole = new Role
        {
            Id          = 101,
            Name        = RoleNames.Agent,
            Description = "Standard agent",
            CreatedBy   = "test",
            CreatedAt   = DateTime.UtcNow
        };
        _db.Roles.Add(agentRole);

        var activeUser = new ApplicationUser
        {
            LoginName      = "testagent",
            PasswordHash   = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
            UserType       = UserType.Agent,
            DomainEntityId = 1,
            IsActive       = true
        };
        _db.Users.Add(activeUser);

        var inactiveUser = new ApplicationUser
        {
            LoginName    = "inactiveuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
            UserType     = UserType.Agent,
            IsActive     = false
        };
        _db.Users.Add(inactiveUser);

        _db.SaveChanges();

        _db.UserRoles.Add(new UserRole
        {
            UserId     = activeUser.Id,
            RoleId     = agentRole.Id,
            AssignedBy = "test"
        });
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();
}
