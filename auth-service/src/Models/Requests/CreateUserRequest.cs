using AuthService.Entities;
using FluentValidation;

namespace AuthService.Models.Requests;

public class CreateUserRequest
{
    public required string LoginName    { get; set; }
    public required string Password     { get; set; }
    public string?         Email        { get; set; }
    public UserType        UserType     { get; set; } = UserType.Agent;
    public int?            DomainEntityId { get; set; }
    public string?         MaxLevel     { get; set; }
    public string?         CreatedBy    { get; set; }
    public List<int>       RoleIds      { get; set; } = [];
    /// <summary>Role names to assign (resolved to IDs at creation time). Used by inter-service provisioning.</summary>
    public List<string>    RoleNames    { get; set; } = [];
}

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.LoginName).NotEmpty().MinimumLength(3).MaximumLength(100);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.");
        RuleFor(x => x.Email).EmailAddress().When(x => x.Email is not null);
    }
}
