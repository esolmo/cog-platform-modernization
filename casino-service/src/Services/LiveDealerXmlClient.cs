using CasinoService.Configuration;
using Microsoft.Extensions.Options;
using System.Net;
using System.Xml.Linq;

namespace CasinoService.Services;

/// <summary>
/// HTTP client that wraps the external Live Dealer XML API at
/// https://ittds.newland.cr/api/connect.php?xmlStr=...
///
/// The API accepts GET requests with a URL-encoded XML payload and responds
/// with an XML document. On failure the response contains a &lt;playerFailure&gt;
/// element with &lt;playerFailureCode&gt; and &lt;playerFailureReason&gt; children.
/// </summary>
public class LiveDealerXmlClient(
    HttpClient httpClient,
    IOptions<CasinoOptions> options,
    ILogger<LiveDealerXmlClient> logger) : ILiveDealerClient
{
    private readonly CasinoOptions _opts = options.Value;

    // ──────────────────────────────────────────────────────────────────────
    // Public interface
    // ──────────────────────────────────────────────────────────────────────

    public async Task<LiveDealerPlayerResult> AddPlayerAsync(
        string custId, string nickname, string countryCode, string ipAddress, string custSource,
        CancellationToken ct = default)
    {
        var body = $"""
            <reqPlayerAdd>
              <custId>{Esc(custId)}</custId>
              <dispName>{Esc(nickname)}</dispName>
              <countryCode>{Esc(countryCode)}</countryCode>
              <playerIPAddress>{Esc(ipAddress)}</playerIPAddress>
              <custsource>{Esc(custSource)}</custsource>
            </reqPlayerAdd>
            """;

        var xml = await SendAsync(body, ct);
        if (xml is null) return Fail<LiveDealerPlayerResult>("TIMEOUT", "External API did not respond");

        if (HasFailure(xml, out var code, out var desc))
            return Fail<LiveDealerPlayerResult>(code, desc);

        return new LiveDealerPlayerResult(
            Success: true,
            CustId: xml.Descendants("custId").FirstOrDefault()?.Value,
            Nickname: xml.Descendants("dispName").FirstOrDefault()?.Value,
            Ticket: xml.Descendants("ticket").FirstOrDefault()?.Value,
            ExternalPlayerId: xml.Descendants("externalPlayerId").FirstOrDefault()?.Value,
            Balance: 0, BonusBalance: 0, Playthrough: 0,
            ErrorCode: null, ErrorDescription: null);
    }

    public async Task<LiveDealerPlayerResult> LoginAsync(
        string custId, string nickname, string ipAddress,
        CancellationToken ct = default)
    {
        var body = $"""
            <reqPlayerLogin>
              <custId>{Esc(custId)}</custId>
              <dispName>{Esc(nickname)}</dispName>
              <playerIPAddress>{Esc(ipAddress)}</playerIPAddress>
            </reqPlayerLogin>
            """;

        var xml = await SendAsync(body, ct);
        if (xml is null) return Fail<LiveDealerPlayerResult>("TIMEOUT", "External API did not respond");

        if (HasFailure(xml, out var code, out var desc))
            return Fail<LiveDealerPlayerResult>(code, desc);

        return new LiveDealerPlayerResult(
            Success: true,
            CustId: custId,
            Nickname: xml.Descendants("dispName").FirstOrDefault()?.Value ?? nickname,
            Ticket: xml.Descendants("ticket").FirstOrDefault()?.Value,
            ExternalPlayerId: xml.Descendants("externalPlayerId").FirstOrDefault()?.Value,
            Balance: ParseDecimal(xml.Descendants("balance").FirstOrDefault()?.Value),
            BonusBalance: ParseDecimal(xml.Descendants("bonusbalance").FirstOrDefault()?.Value),
            Playthrough: ParseDecimal(xml.Descendants("playthrough").FirstOrDefault()?.Value),
            ErrorCode: null, ErrorDescription: null);
    }

    public async Task<LiveDealerBalanceResult> GetBalanceAsync(
        string custId, string nickname,
        CancellationToken ct = default)
    {
        var body = $"""
            <reqPlayerBalance>
              <custId>{Esc(custId)}</custId>
              <dispName>{Esc(nickname)}</dispName>
            </reqPlayerBalance>
            """;

        var xml = await SendAsync(body, ct);
        if (xml is null) return new LiveDealerBalanceResult(false, 0, 0, "TIMEOUT", "External API did not respond");

        if (HasFailure(xml, out var code, out var desc))
            return new LiveDealerBalanceResult(false, 0, 0, code, desc);

        return new LiveDealerBalanceResult(
            Success: true,
            Balance: ParseDecimal(xml.Descendants("balance").FirstOrDefault()?.Value),
            BonusBalance: ParseDecimal(xml.Descendants("bonusbalance").FirstOrDefault()?.Value),
            ErrorCode: null, ErrorDescription: null);
    }

    public async Task<LiveDealerTransferResult> InitTransferAsync(
        string custId, decimal amount, string transferAction,
        int transferReference, string description,
        CancellationToken ct = default)
    {
        var body = $"""
            <reqPlayerFundsTransferInit>
              <custId>{Esc(custId)}</custId>
              <amount>{amount:F2}</amount>
              <transferAction>{Esc(transferAction)}</transferAction>
              <transferReference>{transferReference}</transferReference>
              <desc>{Esc(description)}</desc>
            </reqPlayerFundsTransferInit>
            """;

        var xml = await SendAsync(body, ct);
        if (xml is null) return new LiveDealerTransferResult(false, null, null, "TIMEOUT", "External API did not respond");

        // Treat "processed earlier" as success — idempotency
        if (xml.Descendants("transferProcessedEarlier").FirstOrDefault()?.Value == "Y")
            return new LiveDealerTransferResult(
                Success: true,
                TransferReference: xml.Descendants("transferReference").FirstOrDefault()?.Value,
                RemoteReference: xml.Descendants("remoteReference").FirstOrDefault()?.Value,
                ErrorCode: null, ErrorDescription: null);

        if (HasFailure(xml, out var code, out var desc))
            return new LiveDealerTransferResult(false, null, null, code, desc);

        return new LiveDealerTransferResult(
            Success: true,
            TransferReference: xml.Descendants("transferReference").FirstOrDefault()?.Value,
            RemoteReference: xml.Descendants("remoteReference").FirstOrDefault()?.Value,
            ErrorCode: null, ErrorDescription: null);
    }

    public async Task<LiveDealerTransferResult> ConfirmTransferAsync(
        string custId, string transferAction,
        string transferReference, string remoteReference,
        bool includeConfirmation = false,
        CancellationToken ct = default)
    {
        var confirmNode = includeConfirmation
            ? "<confirmation><confQuestionResponse>N</confQuestionResponse></confirmation>"
            : string.Empty;

        var body = $"""
            <reqPlayerFundsTransferFinal>
              <custId>{Esc(custId)}</custId>
              <transferAction>{Esc(transferAction)}</transferAction>
              <transferReference>{Esc(transferReference)}</transferReference>
              <remoteReference>{Esc(remoteReference)}</remoteReference>
              {confirmNode}
            </reqPlayerFundsTransferFinal>
            """;

        var xml = await SendAsync(body, ct);
        if (xml is null) return new LiveDealerTransferResult(false, null, null, "TIMEOUT", "External API did not respond");

        if (xml.Descendants("transferProcessedEarlier").FirstOrDefault()?.Value == "Y")
            return new LiveDealerTransferResult(
                Success: true,
                TransferReference: transferReference,
                RemoteReference: remoteReference,
                ErrorCode: null, ErrorDescription: null);

        if (HasFailure(xml, out var code, out var desc))
            return new LiveDealerTransferResult(false, null, null, code, desc);

        return new LiveDealerTransferResult(
            Success: true,
            TransferReference: xml.Descendants("transferReference").FirstOrDefault()?.Value ?? transferReference,
            RemoteReference: xml.Descendants("remoteReference").FirstOrDefault()?.Value ?? remoteReference,
            ErrorCode: null, ErrorDescription: null);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────────────

    private async Task<XDocument?> SendAsync(string bodyXml, CancellationToken ct)
    {
        var envelope = $"""<?xml version="1.0" encoding="UTF-8" ?><ch version="1.0"><request>{bodyXml}</request></ch>""";
        var encoded  = WebUtility.UrlEncode(envelope);
        var url      = $"{_opts.ApiBaseUrl}?xmlStr={encoded}";

        try
        {
            logger.LogDebug("[LiveDealer] → {Url}", _opts.ApiBaseUrl);
            var response = await httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(ct);
            logger.LogDebug("[LiveDealer] ← {Content}", content);
            return XDocument.Parse(content);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[LiveDealer] Request failed");
            return null;
        }
    }

    private static bool HasFailure(XDocument xml, out string code, out string desc)
    {
        if (xml.Descendants("playerFailure").Any())
        {
            code = xml.Descendants("playerFailureCode").FirstOrDefault()?.Value ?? "UNKNOWN";
            desc = xml.Descendants("playerFailureReason").FirstOrDefault()?.Value ?? "Unknown error";
            return true;
        }
        code = desc = string.Empty;
        return false;
    }

    private static T Fail<T>(string code, string desc) where T : class
    {
        if (typeof(T) == typeof(LiveDealerPlayerResult))
            return (new LiveDealerPlayerResult(false, null, null, null, null, 0, 0, 0, code, desc) as T)!;
        throw new InvalidOperationException($"Unsupported fail type {typeof(T).Name}");
    }

    private static decimal ParseDecimal(string? value) =>
        decimal.TryParse(value, out var d) ? d : 0m;

    private static string Esc(string? value) =>
        System.Security.SecurityElement.Escape(value ?? string.Empty) ?? string.Empty;
}
