using FluentValidation;

namespace CasinoService.Models.Requests;

public record RegisterPlayerRequest(string Nickname, string IpAddress);

public class RegisterPlayerRequestValidator : AbstractValidator<RegisterPlayerRequest>
{
    public RegisterPlayerRequestValidator()
    {
        RuleFor(x => x.Nickname).NotEmpty().MaximumLength(50);
    }
}
