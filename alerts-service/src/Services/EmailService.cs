using AlertsService.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace AlertsService.Services;

/// <summary>
/// Sends VIP alert notification emails via SMTP using MailKit.
/// Replaces nodemailer SMTP transport from InstantAction/ac/app.js.
/// </summary>
public class EmailService(IConfiguration config, ILogger<EmailService> logger) : IEmailService
{
    public async Task SendVipAlertEmailAsync(
        string toAddress,
        string subject,
        AlertTicketDto ticket,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(toAddress))
        {
            logger.LogDebug("No email address configured; skipping VIP alert email for wager {WagerNumber}", ticket.WagerNumber);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(config["Email:From"] ?? "alerts@cog.local"));
            message.To.Add(MailboxAddress.Parse(toAddress));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = BuildHtmlBody(ticket),
                TextBody = BuildTextBody(ticket)
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                config["Email:Host"] ?? "localhost",
                int.Parse(config["Email:Port"] ?? "587"),
                SecureSocketOptions.StartTlsWhenAvailable,
                ct);

            var user = config["Email:Username"];
            var pass = config["Email:Password"];
            if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
                await smtp.AuthenticateAsync(user, pass, ct);

            await smtp.SendAsync(message, ct);
            await smtp.DisconnectAsync(true, ct);

            logger.LogInformation("Sent VIP alert email to {Email} for wager {WagerNumber}", toAddress, ticket.WagerNumber);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send VIP alert email to {Email} for wager {WagerNumber}", toAddress, ticket.WagerNumber);
        }
    }

    private static string BuildHtmlBody(AlertTicketDto ticket)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("<table cellpadding='4' cellspacing='0' style='font-family:sans-serif;font-size:13px;'>");
        sb.Append($"<tr><td><b>Customer:</b></td><td>{ticket.CustomerLoginName}</td></tr>");
        sb.Append($"<tr><td><b>Wager #:</b></td><td>{ticket.InetWagerNumber}</td></tr>");
        sb.Append($"<tr><td><b>Amount:</b></td><td>{ticket.Amount:C}</td></tr>");

        foreach (var attr in ticket.Attributes)
            sb.Append($"<tr><td><b>{attr.Name}:</b></td><td>{attr.Value}</td></tr>");

        sb.Append($"<tr><td><b>Description:</b></td><td>{ticket.Description}</td></tr>");

        if (ticket.Details.Count > 0)
        {
            sb.Append("<tr><td colspan='2'><b>Details:</b><br/><table cellpadding='2'>");
            foreach (var detail in ticket.Details)
            {
                sb.Append($"<tr><td colspan='2'>{detail.Description}</td></tr>");
                foreach (var a in detail.Attributes)
                    sb.Append($"<tr><td>{a.Name}:</td><td>{a.Value}</td></tr>");
            }
            sb.Append("</table></td></tr>");
        }

        sb.Append("</table>");
        return sb.ToString();
    }

    private static string BuildTextBody(AlertTicketDto ticket)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Customer: {ticket.CustomerLoginName}");
        sb.AppendLine($"Wager #: {ticket.InetWagerNumber}");
        sb.AppendLine($"Amount: {ticket.Amount:C}");
        foreach (var attr in ticket.Attributes)
            sb.AppendLine($"{attr.Name}: {attr.Value}");
        sb.AppendLine($"Description: {ticket.Description}");
        return sb.ToString();
    }
}
