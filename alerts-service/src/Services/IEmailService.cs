using AlertsService.Models;

namespace AlertsService.Services;

public interface IEmailService
{
    Task SendVipAlertEmailAsync(string toAddress, string subject, AlertTicketDto ticket, CancellationToken ct = default);
}
