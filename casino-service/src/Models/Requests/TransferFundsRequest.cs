using FluentValidation;

namespace CasinoService.Models.Requests;

public record TransferFundsRequest(decimal Amount);

public class TransferFundsRequestValidator : AbstractValidator<TransferFundsRequest>
{
    public TransferFundsRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).PrecisionScale(18, 2, false);
    }
}
