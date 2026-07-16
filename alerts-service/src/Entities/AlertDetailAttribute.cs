namespace AlertsService.Entities;

/// <summary>
/// Key-value attribute on an alert detail leg.
/// </summary>
public class AlertDetailAttribute
{
    public int Id { get; set; }
    public int AlertDetailId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public AlertDetail AlertDetail { get; set; } = null!;
}
