namespace AccountsService.Entities;

/// <summary>
/// Per-customer product feature flags.
/// Replaces the legacy BitPermission / BitAccumulator bitmask columns on the Customer table.
/// Each flag maps to a bit in the legacy system (see Constants.asp).
/// </summary>
public class CustomerPermissions
{
    public int  CustomerId          { get; set; }

    // Sports book channels
    public bool WebSportsEnabled    { get; set; } = true;
    public bool CallInEnabled       { get; set; } = true;
    public bool InternetEnabled     { get; set; } = true;

    // Product access
    public bool RacebookEnabled     { get; set; } = true;
    public bool CasinoEnabled       { get; set; } = true;
    public bool LotteryEnabled      { get; set; } = true;
    public bool LiveDealerEnabled   { get; set; } = true;
    public bool HorseEnabled        { get; set; } = true;

    // Wager type permissions
    public bool ParlayEnabled       { get; set; } = true;
    public bool TeaserEnabled       { get; set; } = true;
    public bool IfBetEnabled        { get; set; } = true;
    public bool ReverseEnabled      { get; set; } = true;

    // Account flags
    public bool AccountLocked       { get; set; } = false;  // full account suspension
    public bool ReceiveAlerts       { get; set; } = true;

    public DateTime UpdatedAt       { get; set; } = DateTime.UtcNow;
    public string   UpdatedBy       { get; set; } = string.Empty;

    // Navigation
    public Customer Customer { get; set; } = null!;
}
