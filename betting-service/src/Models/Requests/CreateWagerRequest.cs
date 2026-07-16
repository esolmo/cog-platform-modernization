using Cog.Domain.Entities;
using FluentValidation;

namespace BettingService.Models.Requests;

public class CreateWagerRequest
{
    public int CustomerId { get; set; }
    public WagerType WagerType { get; set; }
    public decimal RiskAmount { get; set; }
    public string? IdempotencyKey { get; set; }
    public List<WagerItemRequest> Items { get; set; } = [];
}

public class WagerItemRequest
{
    public int GamePeriodId { get; set; }
    public WagerItemType ItemType { get; set; }
    public WagerSide Side { get; set; }
}

public class CreateWagerRequestValidator : AbstractValidator<CreateWagerRequest>
{
    public CreateWagerRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.RiskAmount).GreaterThan(0).LessThanOrEqualTo(100_000);
        RuleFor(x => x.Items).NotEmpty().WithMessage("A wager must have at least one item.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.GamePeriodId).GreaterThan(0);
        });

        When(x => x.WagerType == WagerType.Straight, () =>
            RuleFor(x => x.Items).Must(i => i.Count == 1)
                .WithMessage("Straight wager must have exactly one item."));

        When(x => x.WagerType == WagerType.Parlay, () =>
            RuleFor(x => x.Items).Must(i => i.Count is >= 2 and <= 15)
                .WithMessage("Parlay wager must have 2–15 items."));
    }
}
