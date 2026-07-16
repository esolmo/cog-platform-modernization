using FluentValidation;

namespace AuthService.Models.Requests;

public class RefreshTokenRequest
{
    public required string RefreshToken { get; set; }
}

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
