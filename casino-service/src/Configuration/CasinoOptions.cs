namespace CasinoService.Configuration;

public class CasinoOptions
{
    public const string Section = "Casino";

    /// <summary>External Live Dealer XML API base URL.</summary>
    public string ApiBaseUrl { get; set; } = "https://ittds.newland.cr/api/connect.php";

    /// <summary>Lobby redirect base URL (appended with session ticket).</summary>
    public string LobbyBaseUrl { get; set; } = "https://ittds.newland.cr/entrance/playervalid.php";

    /// <summary>Source identifier sent to the external provider.</summary>
    public string CustomerSource { get; set; } = "itds";

    /// <summary>Default country code sent on player registration.</summary>
    public string DefaultCountryCode { get; set; } = "US";

    /// <summary>Internal casino identifier (2 = Live Dealer).</summary>
    public int CasinoId { get; set; } = 2;
}
