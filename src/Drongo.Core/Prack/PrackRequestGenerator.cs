using System.Collections.Generic;
using Drongo.Core.Messages;

namespace Drongo.Core.Prack;

/// <summary>
/// Generates PRACK requests per RFC 3262.
/// </summary>
public class PrackRequestGenerator
{
    private readonly RSeqRackHeaderGenerator _headerGenerator = new();

    /// <summary>
    /// Generates a PRACK request to acknowledge a provisional response.
    /// </summary>
    public SipRequest GeneratePrackRequest(
        string dialogId,
        int rseq,
        string method,
        int cseq,
        SipUri targetUri,
        SipUri localUri,
        SipUri remoteUri,
        Dictionary<string, string> routeHeaders,
        string? viaHeader = null,
        string? contactHeader = null)
    {
        var headers = new Dictionary<string, string>();

        var rackInfo = new RackInfo
        {
            RSeq = rseq,
            Method = method,
            CSeq = cseq
        };
        headers["Rack"] = _headerGenerator.GenerateRackHeader(rackInfo);

        headers["From"] = localUri.ToString();
        headers["To"] = remoteUri.ToString();
        headers["Call-ID"] = dialogId;
        headers["CSeq"] = $"{cseq + 1} PRACK";
        headers["Max-Forwards"] = "70";

        if (!string.IsNullOrEmpty(viaHeader))
        {
            headers["Via"] = viaHeader;
        }

        if (!string.IsNullOrEmpty(contactHeader))
        {
            headers["Contact"] = contactHeader;
        }

        foreach (var route in routeHeaders)
        {
            headers[route.Key] = route.Value;
        }

        return new SipRequest(
            SipMethod.Prack,
            targetUri,
            "SIP/2.0",
            headers
        );
    }
}

/// <summary>
/// Parses PRACK requests to extract Rack information.
/// </summary>
public class PrackRequestParser
{
    private readonly RSeqRackHeaderParser _headerParser = new();

    /// <summary>
    /// Parses a PRACK request to extract Rack information (RSeq, method, CSeq).
    /// </summary>
    public RackInfo ParseRackFromRequest(SipRequest request)
    {
        var rackHeader = request.TryGetHeader("Rack")
            ?? throw new InvalidOperationException("PRACK request missing Rack header");

        return _headerParser.ParseRackHeader(rackHeader);
    }
}
