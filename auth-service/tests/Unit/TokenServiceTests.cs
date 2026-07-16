using Xunit;
using AuthService.Configuration;
using AuthService.Entities;
using AuthService.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AuthService.Tests.Unit;

public class TokenServiceTests
{
    private readonly TokenService _sut;
    private readonly JwtOptions   _options;

    public TokenServiceTests()
    {
        _options = new JwtOptions
        {
            SecretKey                = "SuperSecretKeyForTestingPurposesOnly123!",
            Issuer                   = "cog-auth-service",
            Audience                 = "cog-services",
            AccessTokenExpiryMinutes = 15,
            RefreshTokenExpiryDays   = 7
        };
        _sut = new TokenService(Options.Create(_options), NullLogger<TokenService>.Instance);
    }

    [Fact]
    public void GenerateAccessToken_WithValidUser_ReturnsNonEmptyToken()
    {
        var user = BuildTestUser();
        var roles = new[] { RoleNames.Agent };
        var permissions = new[] { PermissionNames.WagersCreate, PermissionNames.WagersRead };

        var token = _sut.GenerateAccessToken(user, roles, permissions);

        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3, "JWT must have three dot-separated parts");
    }

    [Fact]
    public void ValidateAccessToken_WithValidToken_ReturnsUserIdAndLoginName()
    {
        var user  = BuildTestUser();
        var token = _sut.GenerateAccessToken(user, [RoleNames.Agent], []);

        var isValid = _sut.ValidateAccessToken(token, out var userId, out var loginName);

        isValid.Should().BeTrue();
        userId.Should().Be(user.Id);
        loginName.Should().Be(user.LoginName);
    }

    [Fact]
    public void ValidateAccessToken_WithTamperedToken_ReturnsFalse()
    {
        var user  = BuildTestUser();
        var token = _sut.GenerateAccessToken(user, [], []);
        var tampered = token[..^5] + "XXXXX";

        var isValid = _sut.ValidateAccessToken(tampered, out _, out _);

        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateAccessToken_WithEmptyToken_ReturnsFalse()
    {
        var isValid = _sut.ValidateAccessToken(string.Empty, out _, out _);
        isValid.Should().BeFalse();
    }

    [Fact]
    public void GenerateRefreshToken_ProducesUniqueTokens()
    {
        var token1 = _sut.GenerateRefreshToken();
        var token2 = _sut.GenerateRefreshToken();

        token1.Should().NotBe(token2);
    }

    [Fact]
    public void HashToken_SameInputProducesSameHash()
    {
        const string raw = "my-test-token";

        var hash1 = _sut.HashToken(raw);
        var hash2 = _sut.HashToken(raw);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void HashToken_DifferentInputsProduceDifferentHashes()
    {
        var hash1 = _sut.HashToken("token-one");
        var hash2 = _sut.HashToken("token-two");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void GenerateAccessToken_IncludesRolesAndPermissionsAsClaims()
    {
        var user        = BuildTestUser();
        var roles       = new[] { RoleNames.Agent, RoleNames.LinesManager };
        var permissions = new[] { PermissionNames.LinesRead, PermissionNames.LinesWrite };

        var token   = _sut.GenerateAccessToken(user, roles, permissions);
        var isValid = _sut.ValidateAccessToken(token, out var userId, out _);

        isValid.Should().BeTrue();
        userId.Should().Be(user.Id);
    }

    private static ApplicationUser BuildTestUser() => new()
    {
        Id             = 42,
        LoginName      = "testagent",
        PasswordHash   = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
        UserType       = UserType.Agent,
        DomainEntityId = 10,
        IsActive       = true
    };
}
