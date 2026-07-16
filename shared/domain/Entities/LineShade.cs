namespace Cog.Domain.Entities;

/// <summary>
/// Per-agent line adjustment (shade) applied on top of the base LineSet.
/// Migrated from LM_SetSpread / LM_SetMoneyLine / LM_SetTotal stored procedures.
/// </summary>
public class LineShade
{
    public int Id { get; set; }
    public int LineSetId { get; set; }
    public int AgentId { get; set; }
    public decimal? SpreadAdjustment { get; set; }
    public decimal? HomeMoneyLineAdjustment { get; set; }
    public decimal? AwayMoneyLineAdjustment { get; set; }
    public decimal? TotalAdjustment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public LineSet LineSet { get; set; } = null!;
    public Agent Agent { get; set; } = null!;
}
