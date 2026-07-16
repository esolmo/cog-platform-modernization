namespace AlertsService.Entities;

/// <summary>
/// Detail row for a multi-leg wager in an alert (migrated from CRowDtt in COGLib).
/// </summary>
public class AlertDetail
{
    public int Id { get; set; }
    public int AlertTicketId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string SportKey { get; set; } = string.Empty;

    public AlertTicket AlertTicket { get; set; } = null!;
    public ICollection<AlertDetailAttribute> Attributes { get; set; } = [];
}
