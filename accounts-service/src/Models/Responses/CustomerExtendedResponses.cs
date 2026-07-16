namespace AccountsService.Models.Responses;

public class CustomerPermissionsResponse
{
    public int  CustomerId          { get; set; }
    public bool WebSportsEnabled    { get; set; }
    public bool CallInEnabled       { get; set; }
    public bool InternetEnabled     { get; set; }
    public bool RacebookEnabled     { get; set; }
    public bool CasinoEnabled       { get; set; }
    public bool LotteryEnabled      { get; set; }
    public bool LiveDealerEnabled   { get; set; }
    public bool HorseEnabled        { get; set; }
    public bool ParlayEnabled       { get; set; }
    public bool TeaserEnabled       { get; set; }
    public bool IfBetEnabled        { get; set; }
    public bool ReverseEnabled      { get; set; }
    public bool AccountLocked       { get; set; }
    public bool ReceiveAlerts       { get; set; }
    public DateTime UpdatedAt       { get; set; }
    public string   UpdatedBy       { get; set; } = string.Empty;
}

public class CustomerWagerLimitsResponse
{
    public int     CustomerId        { get; set; }
    public decimal MaxStraightWager  { get; set; }
    public decimal MaxParlayWager    { get; set; }
    public decimal MaxParlayPayout   { get; set; }
    public decimal MaxTeaserWager    { get; set; }
    public decimal MaxIfBetWager     { get; set; }
    public decimal MaxLotteryPick3   { get; set; }
    public decimal MaxLotteryPick4   { get; set; }
    public int     MaxParlayLegs     { get; set; }
    public int     MinimumWager      { get; set; }
}

public class CustomerCasinoLimitsResponse
{
    public int     CustomerId        { get; set; }
    public decimal CasinoWagerLimit  { get; set; }
    public decimal CasinoCreditLimit { get; set; }
}

public class CustomerCommentResponse
{
    public int      Id               { get; set; }
    public int      CustomerId       { get; set; }
    public string   Body             { get; set; } = string.Empty;
    public bool     VisibleToCustomer { get; set; }
    public bool     VisibleToAgent   { get; set; }
    public DateTime CreatedAt        { get; set; }
    public string   CreatedBy        { get; set; } = string.Empty;
}

public class CustomerFreePlayResponse
{
    public int      Id              { get; set; }
    public int      CustomerId      { get; set; }
    public decimal  Amount          { get; set; }
    public string   Description     { get; set; } = string.Empty;
    public DateTime IssuedAt        { get; set; }
    public string   IssuedBy        { get; set; } = string.Empty;
    public DateTime? ExpiresAt      { get; set; }
    public bool     IsRedeemed      { get; set; }
    public decimal  RedeemedAmount  { get; set; }
}
