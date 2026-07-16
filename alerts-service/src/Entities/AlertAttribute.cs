namespace AlertsService.Entities;

/// <summary>
/// Key-value attribute on an alert ticket (migrated from CRowAtt in COGLib).
/// </summary>
public class AlertAttribute
{
    public int Id { get; set; }
    public int AlertTicketId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public AlertTicket AlertTicket { get; set; } = null!;
}
