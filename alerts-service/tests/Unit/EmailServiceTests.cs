using AlertsService.Models;
using AlertsService.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace AlertsService.Tests.Unit;

public class EmailServiceTests
{
    private readonly EmailService _sut;

    public EmailServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:From"] = "test@cog.local",
                ["Email:Host"] = "localhost",
                ["Email:Port"] = "25"
            })
            .Build();

        _sut = new EmailService(config, NullLogger<EmailService>.Instance);
    }

    [Fact]
    public async Task SendVipAlertEmail_DoesNotThrow_WhenEmailAddressIsEmpty()
    {
        var ticket = MakeTicket();
        // Should silently skip when no email address — no exception
        var act = async () => await _sut.SendVipAlertEmailAsync("", "Test Subject", ticket);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendVipAlertEmail_DoesNotThrow_WhenSmtpUnavailable()
    {
        var ticket = MakeTicket();
        // SMTP is not running locally — service should catch and log, not throw
        var act = async () => await _sut.SendVipAlertEmailAsync("agent@example.com", "Test", ticket);
        await act.Should().NotThrowAsync();
    }

    private static AlertTicketDto MakeTicket() =>
        new(
            Id: 1,
            WagerNumber: 12345,
            AgentId: 1,
            CustomerLoginName: "CUST01",
            InetWagerNumber: "W12345",
            WagerType: "Straight",
            Amount: 500.00m,
            Description: "Dallas Cowboys -3",
            IsVipAlert: true,
            IsSharpAction: false,
            IsSquareAction: false,
            InsertedAt: DateTime.UtcNow,
            Attributes:
            [
                new AttributeDto("Sport", "NFL"),
                new AttributeDto("Game", "DAL vs NYG")
            ],
            Details:
            [
                new AlertDetailDto("Dallas Cowboys -3 (500)", "NFL", [new AttributeDto("Odds", "-110")])
            ]
        );
}
