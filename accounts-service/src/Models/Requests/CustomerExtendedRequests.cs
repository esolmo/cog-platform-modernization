namespace AccountsService.Models.Requests;

public class UpdateCustomerPermissionsRequest
{
    public bool? WebSportsEnabled   { get; set; }
    public bool? CallInEnabled      { get; set; }
    public bool? InternetEnabled    { get; set; }
    public bool? RacebookEnabled    { get; set; }
    public bool? CasinoEnabled      { get; set; }
    public bool? LotteryEnabled     { get; set; }
    public bool? LiveDealerEnabled  { get; set; }
    public bool? HorseEnabled       { get; set; }
    public bool? ParlayEnabled      { get; set; }
    public bool? TeaserEnabled      { get; set; }
    public bool? IfBetEnabled       { get; set; }
    public bool? ReverseEnabled     { get; set; }
    public bool? AccountLocked      { get; set; }
    public bool? ReceiveAlerts      { get; set; }
    public string UpdatedBy         { get; set; } = string.Empty;
}

public class UpdateCustomerWagerLimitsRequest
{
    public decimal? MaxStraightWager    { get; set; }
    public decimal? MaxParlayWager      { get; set; }
    public decimal? MaxParlayPayout     { get; set; }
    public decimal? MaxTeaserWager      { get; set; }
    public decimal? MaxIfBetWager       { get; set; }
    public decimal? MaxLotteryPick3     { get; set; }
    public decimal? MaxLotteryPick4     { get; set; }
    public int?     MaxParlayLegs       { get; set; }
    public int?     MinimumWager        { get; set; }
    public string   UpdatedBy           { get; set; } = string.Empty;
}

public class UpdateCustomerCasinoLimitsRequest
{
    public decimal? CasinoWagerLimit    { get; set; }
    public decimal? CasinoCreditLimit   { get; set; }
    public string   UpdatedBy           { get; set; } = string.Empty;
}

public class UpdateCustomerSettleFigureRequest
{
    public decimal  SettleFigure        { get; set; }
    public string   UpdatedBy           { get; set; } = string.Empty;
}

public class ReassignCustomerAgentRequest
{
    public int    NewAgentId            { get; set; }
    public bool   InheritAgentSettings  { get; set; } = false;
    public string UpdatedBy             { get; set; } = string.Empty;
}

public class AwardFreePlayRequest
{
    public decimal  Amount              { get; set; }
    public string   Description         { get; set; } = string.Empty;
    public DateTime? ExpiresAt          { get; set; }
    public string   IssuedBy            { get; set; } = string.Empty;
}

public class AddCustomerCommentRequest
{
    public string   Body                { get; set; } = string.Empty;
    public bool     VisibleToCustomer   { get; set; } = false;
    public bool     VisibleToAgent      { get; set; } = true;
    public string   CreatedBy           { get; set; } = string.Empty;
}

public class BatchTransactionItem
{
    public int      CustomerId          { get; set; }
    public string   Type                { get; set; } = string.Empty;  // Credit | Debit
    public decimal  Amount              { get; set; }
    public string?  Note                { get; set; }
}

public class BatchTransactionRequest
{
    public List<BatchTransactionItem> Transactions { get; set; } = [];
    public string   CreatedBy           { get; set; } = string.Empty;
}
