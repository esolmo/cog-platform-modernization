namespace BettingService.Models.Requests;

public class SetSpreadRequest
{
    public decimal Spread { get; set; }
    public decimal Juice { get; set; } = -110;
}

public class SetMoneyLineRequest
{
    public decimal HomeMoneyLine { get; set; }
    public decimal AwayMoneyLine { get; set; }
}

public class SetTotalRequest
{
    public decimal Total { get; set; }
    public decimal OverJuice { get; set; } = -110;
    public decimal UnderJuice { get; set; } = -110;
}

public class ApplyShadeRequest
{
    public int AgentId { get; set; }
    public decimal? SpreadAdjustment { get; set; }
    public decimal? HomeMoneyLineAdjustment { get; set; }
    public decimal? AwayMoneyLineAdjustment { get; set; }
    public decimal? TotalAdjustment { get; set; }
}
