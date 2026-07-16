namespace Cog.Domain.Entities;

public class LineSet
{
    public int Id { get; set; }
    public int GamePeriodId { get; set; }
    public decimal? Spread { get; set; }
    public decimal? SpreadJuice { get; set; }
    public decimal? HomeMoneyLine { get; set; }
    public decimal? AwayMoneyLine { get; set; }
    public decimal? Total { get; set; }
    public decimal? OverJuice { get; set; }
    public decimal? UnderJuice { get; set; }
    public bool OfferingSpread { get; set; } = true;
    public bool OfferingMoneyLine { get; set; } = true;
    public bool OfferingTotal { get; set; } = true;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }

    public GamePeriod GamePeriod { get; set; } = null!;
    public ICollection<LineShade> Shades { get; set; } = new List<LineShade>();
}
