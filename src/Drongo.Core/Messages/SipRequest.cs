using System.Text;

namespace Drongo.Core.Messages;

public sealed class SipRequest
{
    public SipMethod Method { get; init; }
    public SipUri RequestUri { get; init; }
    public string SipVersion { get; init; }
    public IReadOnlyDictionary<string, string> Headers { get; init; }
    public ReadOnlyMemory<byte> Body { get; init; }

    public string CallId => GetHeader("Call-ID");
    public string From => GetHeader("From");
    public string To => GetHeader("To");
    public string CSeq => GetHeader("CSeq");
    public string Via => GetHeader("Via");
    public string? Contact => TryGetHeader("Contact");
    public string? MaxForwards => TryGetHeader("Max-Forwards");
    public string? ContentType => TryGetHeader("Content-Type");
    public int? ContentLength => TryGetHeader("Content-Length") is { } cl && int.TryParse(cl, out var len) ? len : null;

    public bool HasBody => !Body.IsEmpty;

    public SipRequest(
        SipMethod method,
        SipUri requestUri,
        string sipVersion,
        IReadOnlyDictionary<string, string> headers,
        ReadOnlyMemory<byte> body = default)
    {
        Method = method;
        RequestUri = requestUri;
        SipVersion = sipVersion;
        Headers = headers ?? new Dictionary<string, string>();
        Body = body;
    }

    public string GetHeader(string name) => Headers.TryGetValue(name, out var value) 
        ? value 
        : throw new InvalidOperationException($"Missing required header: {name}");

    public string? TryGetHeader(string name) => Headers.TryGetValue(name, out var value) ? value : null;

    public bool HasHeader(string name) => Headers.ContainsKey(name);

    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"{Method.ToMethodString()} {RequestUri} {SipVersion}");
        
        foreach (var (key, value) in Headers)
        {
            sb.AppendLine($"{key}: {value}");
        }
        
        if (HasBody)
        {
            sb.AppendLine();
            sb.Append(Encoding.ASCII.GetString(Body.Span));
        }
        
        return sb.ToString();
    }
}
