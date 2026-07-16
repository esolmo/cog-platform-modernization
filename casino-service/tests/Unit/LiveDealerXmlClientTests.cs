using Xunit;
using CasinoService.Configuration;
using CasinoService.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;

namespace CasinoService.Tests.Unit;

public class LiveDealerXmlClientTests
{
    private static LiveDealerXmlClient CreateClient(string responseXml)
    {
        var handler = new MockHttpMessageHandler(responseXml);
        var httpClient = new HttpClient(handler);
        var opts = Options.Create(new CasinoOptions
        {
            ApiBaseUrl = "https://fake-dealer.test/api/connect.php",
            LobbyBaseUrl = "https://fake-dealer.test/entrance/playervalid.php",
            CustomerSource = "itds",
            DefaultCountryCode = "US"
        });
        return new LiveDealerXmlClient(httpClient, opts, NullLogger<LiveDealerXmlClient>.Instance);
    }

    [Fact]
    public async Task AddPlayer_SuccessResponse_ReturnsPlayerData()
    {
        const string xml = """
            <?xml version="1.0" encoding="UTF-8" ?>
            <ch><response>
              <custId>C001</custId>
              <dispName>Tester</dispName>
              <ticket>tok-001</ticket>
              <externalPlayerId>ext-001</externalPlayerId>
            </response></ch>
            """;

        var client = CreateClient(xml);
        var result = await client.AddPlayerAsync("C001", "Tester", "US", "127.0.0.1", "itds");

        result.Success.Should().BeTrue();
        result.CustId.Should().Be("C001");
        result.Nickname.Should().Be("Tester");
        result.ExternalPlayerId.Should().Be("ext-001");
    }

    [Fact]
    public async Task AddPlayer_FailureResponse_ReturnsError()
    {
        const string xml = """
            <?xml version="1.0" encoding="UTF-8" ?>
            <ch><response>
              <playerFailure>
                <playerFailureCode>201</playerFailureCode>
                <playerFailureReason>Display name already exists</playerFailureReason>
              </playerFailure>
            </response></ch>
            """;

        var client = CreateClient(xml);
        var result = await client.AddPlayerAsync("C001", "Taken", "US", "127.0.0.1", "itds");

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("201");
        result.ErrorDescription.Should().Contain("already exists");
    }

    [Fact]
    public async Task Login_SuccessResponse_ReturnsBalanceAndTicket()
    {
        const string xml = """
            <?xml version="1.0" encoding="UTF-8" ?>
            <ch><response>
              <dispName>Tester</dispName>
              <ticket>session-tok</ticket>
              <externalPlayerId>ext-001</externalPlayerId>
              <balance>250.00</balance>
              <bonusbalance>0.00</bonusbalance>
              <playthrough>0</playthrough>
            </response></ch>
            """;

        var client = CreateClient(xml);
        var result = await client.LoginAsync("C001", "Tester", "127.0.0.1");

        result.Success.Should().BeTrue();
        result.Ticket.Should().Be("session-tok");
        result.Balance.Should().Be(250m);
    }

    [Fact]
    public async Task InitTransfer_ProcessedEarlier_ReturnsSuccess()
    {
        const string xml = """
            <?xml version="1.0" encoding="UTF-8" ?>
            <ch><response>
              <transferProcessedEarlier>Y</transferProcessedEarlier>
              <transferReference>REF-001</transferReference>
              <remoteReference>REM-001</remoteReference>
            </response></ch>
            """;

        var client = CreateClient(xml);
        var result = await client.InitTransferAsync("C001", 50m, "BUY", 12345, "chip buy");

        result.Success.Should().BeTrue();
        result.TransferReference.Should().Be("REF-001");
    }

    // ── Mock HTTP handler ──────────────────────────────────────────────────

    private sealed class MockHttpMessageHandler(string responseBody) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/xml")
            });
    }
}
