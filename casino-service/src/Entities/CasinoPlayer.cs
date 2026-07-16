namespace CasinoService.Entities;

public class CasinoPlayer
{
    public int Id { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string ExternalPlayerId { get; set; } = string.Empty;
    public int CasinoId { get; set; } = 2;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public ICollection<CasinoTransaction> Transactions { get; set; } = [];
}
