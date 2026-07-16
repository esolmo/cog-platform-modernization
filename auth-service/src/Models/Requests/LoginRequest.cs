using FluentValidation;

namespace AuthService.Models.Requests;

public class LoginRequest
{
    public required string LoginName { get; set; }
    public required string Password  { get; set; }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.LoginName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}
