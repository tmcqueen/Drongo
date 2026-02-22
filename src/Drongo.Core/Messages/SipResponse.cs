using System.Text;

namespace Drongo.Core.Messages;

public sealed record SipResponse
{
    public int StatusCode { get; init; }
    public string ReasonPhrase { get; init; }
    public string SipVersion { get; init; }
    public IReadOnlyDictionary<string, string> Headers { get; init; }
    public ReadOnlyMemory<byte> Body { get; init; }

    public string CallId => GetHeader("Call-ID");
    public string From => GetHeader("From");
    public string To => GetHeader("To");
    public string CSeq => GetHeader("CSeq");
    public string Via => GetHeader("Via");
    public string? Contact => TryGetHeader("Contact");
    public string? ContentType => TryGetHeader("Content-Type");
    public int? ContentLength => TryGetHeader("Content-Length") is { } cl && int.TryParse(cl, out var len) ? len : null;

    public bool IsProvisional => StatusCode is >= 100 and < 200;
    public bool IsSuccess => StatusCode is >= 200 and < 300;
    public bool IsRedirection => StatusCode is >= 300 and < 400;
    public bool IsClientError => StatusCode is >= 400 and < 500;
    public bool IsServerError => StatusCode is >= 500 and < 600;
    public bool IsGlobalError => StatusCode is >= 600 and < 700;
    public bool HasBody => !Body.IsEmpty;

    public SipResponse(
        int statusCode,
        string reasonPhrase,
        string sipVersion,
        IReadOnlyDictionary<string, string> headers,
        ReadOnlyMemory<byte> body = default)
    {
        StatusCode = statusCode;
        ReasonPhrase = reasonPhrase;
        SipVersion = sipVersion;
        Headers = headers ?? new Dictionary<string, string>();
        Body = body;
    }

    public string GetHeader(string name) => Headers.TryGetValue(name, out var value)
        ? value
        : throw new InvalidOperationException($"Missing required header: {name}");

    public string? TryGetHeader(string name) => Headers.TryGetValue(name, out var value) ? value : null;

    public bool HasHeader(string name) => Headers.ContainsKey(name);

    // Records auto-generate equality based on all properties. ReadOnlyMemory<byte> uses
    // reference/span equality by default, so we override to use byte-sequence equality
    // for Body and structural equality for Headers.
    public bool Equals(SipResponse? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return StatusCode == other.StatusCode
            && ReasonPhrase == other.ReasonPhrase
            && SipVersion == other.SipVersion
            && Headers.Count == other.Headers.Count
            && !Headers.Except(other.Headers).Any()
            && Body.Span.SequenceEqual(other.Body.Span);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(StatusCode);
        hash.Add(ReasonPhrase);
        hash.Add(SipVersion);
        foreach (var (k, v) in Headers.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            hash.Add(k);
            hash.Add(v);
        }
        hash.AddBytes(Body.Span);
        return hash.ToHashCode();
    }

    public static SipResponse Create(
        int statusCode,
        string reasonPhrase,
        IReadOnlyDictionary<string, string>? headers = null,
        ReadOnlyMemory<byte>? body = null,
        string sipVersion = "SIP/2.0")
    {
        return new SipResponse(statusCode, reasonPhrase, sipVersion, headers ?? new Dictionary<string, string>(), body ?? ReadOnlyMemory<byte>.Empty);
    }

    public static SipResponse CreateTrying(IReadOnlyDictionary<string, string> headers) =>
        Create(100, "Trying", headers);

    public static SipResponse CreateRinging(IReadOnlyDictionary<string, string> headers) =>
        Create(180, "Ringing", headers);

    public static SipResponse CreateOk(IReadOnlyDictionary<string, string> headers, ReadOnlyMemory<byte>? body = null) =>
        Create(200, "OK", headers, body);

    public static SipResponse CreateBusyHere(IReadOnlyDictionary<string, string> headers) =>
        Create(486, "Busy Here", headers);

    public static SipResponse CreateNotFound(IReadOnlyDictionary<string, string> headers) =>
        Create(404, "Not Found", headers);

    public static SipResponse CreateBadRequest(string reason, IReadOnlyDictionary<string, string> headers) =>
        Create(400, reason, headers);

    public static SipResponse CreateServerError(IReadOnlyDictionary<string, string> headers) =>
        Create(500, "Server Internal Error", headers);

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{SipVersion} {StatusCode} {ReasonPhrase}");

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
