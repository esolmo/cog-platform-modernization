namespace AccountsService.Models.Responses;

public class AgentDistributionResponse
{
    public int      Id                  { get; set; }
    public int      AgentId             { get; set; }
    public string   AgentLoginName      { get; set; } = string.Empty;
    public DateTime WeekEnding          { get; set; }

    public decimal  WinAmount           { get; set; }
    public decimal  LossAmount          { get; set; }
    public decimal  NetAmount           { get; set; }

    public decimal  CasinoWinAmount     { get; set; }
    public decimal  CasinoLossAmount    { get; set; }
    public decimal  CasinoFeeAmount     { get; set; }

    public decimal  LiveDealerWin       { get; set; }
    public decimal  LiveDealerLoss      { get; set; }
    public decimal  LiveDealerFee       { get; set; }

    public decimal  CreditAdjustments   { get; set; }
    public decimal  DebitAdjustments    { get; set; }

    public string   CommissionType      { get; set; } = string.Empty;
    public decimal  CommissionRate      { get; set; }
    public decimal  CommissionAmount    { get; set; }

    public decimal  PreviousMakeup      { get; set; }
    public decimal  NewMakeup           { get; set; }
    public decimal  HeadCountFee        { get; set; }
    public int      ActivePlayerCount   { get; set; }
    public decimal  NewBalance          { get; set; }

    public bool     IsConfirmed         { get; set; }
    public DateTime CalculatedAt        { get; set; }
    public DateTime? ConfirmedAt        { get; set; }
}

public class AgentMakeupResponse
{
    public int      AgentId             { get; set; }
    public string   AgentLoginName      { get; set; } = string.Empty;
    public decimal  CurrentMakeup       { get; set; }
    public DateTime? AsOfWeekEnding     { get; set; }
}
