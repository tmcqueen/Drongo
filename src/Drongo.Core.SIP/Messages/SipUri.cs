namespace Drongo.Core.SIP.Messages;

public sealed record SipUri
{
    public string Scheme { get; init; }
    public string? User { get; init; }
    public string Host { get; init; }
    public int Port { get; init; }
    public string? Parameters { get; init; }
    public string? Headers { get; init; }

    public SipUri(string scheme, string host, int port = 0, string? user = null, string? parameters = null, string? headers = null)
    {
        Scheme = scheme.ToLowerInvariant();
        Host = host.ToLowerInvariant();
        Port = port;
        User = user?.ToLowerInvariant();
        Parameters = parameters;
        Headers = headers;
    }

    public static SipUri Parse(string uriString)
    {
        if (string.IsNullOrWhiteSpace(uriString))
            throw new SipParseException("URI cannot be empty");

        var uri = uriString.Trim();
        
        var angleBracketStart = uri.IndexOf('<');
        var angleBracketEnd = uri.IndexOf('>');
        
        if (angleBracketStart >= 0 && angleBracketEnd > angleBracketStart)
        {
            uri = uri[(angleBracketStart + 1)..angleBracketEnd];
        }
        
        var schemeEnd = uri.IndexOf(':');
        if (schemeEnd < 0)
            throw new SipParseException($"Invalid URI scheme: {uriString}");

        var scheme = uri[..schemeEnd].ToLowerInvariant();
        if (scheme != "sip" && scheme != "sips")
            throw new SipParseException($"Unsupported URI scheme: {scheme}");

        var rest = uri[(schemeEnd + 1)..];
        
        if (rest.StartsWith("//"))
            rest = rest[2..];

        var headersStart = rest.IndexOf('?');
        string? headers = null;
        if (headersStart >= 0)
        {
            headers = rest[(headersStart + 1)..];
            rest = rest[..headersStart];
        }

        var parametersStart = rest.IndexOf(';');
        string? parameters = null;
        if (parametersStart >= 0)
        {
            parameters = rest[(parametersStart + 1)..];
            rest = rest[..parametersStart];
        }

        var portStart = rest.LastIndexOf(':');
        string host;
        int port = 0;

        if (portStart > 0)
        {
            host = rest[..portStart];
            if (!int.TryParse(rest[(portStart + 1)..], out port))
                throw new SipParseException($"Invalid port in URI: {uriString}");
        }
        else
        {
            host = rest;
        }

        var userStart = host.IndexOf('@');
        string? user = null;
        if (userStart > 0)
        {
            user = host[..userStart];
            host = host[(userStart + 1)..];
        }

        return new SipUri(scheme, host, port, user, parameters, headers);
    }

    public override string ToString()
    {
        // StringBuilder with an estimated capacity avoids repeated string allocations
        // on every AOR key generation, registration lookup, and log line (Drongo-cpb).
        var sb = new System.Text.StringBuilder(64);
        sb.Append(Scheme);
        sb.Append(':');

        if (!string.IsNullOrEmpty(User))
        {
            sb.Append(User);
            sb.Append('@');
        }

        sb.Append(Host);

        if (Port > 0)
        {
            sb.Append(':');
            sb.Append(Port);
        }

        if (!string.IsNullOrEmpty(Parameters))
        {
            sb.Append(';');
            sb.Append(Parameters);
        }

        if (!string.IsNullOrEmpty(Headers))
        {
            sb.Append('?');
            sb.Append(Headers);
        }

        return sb.ToString();
    }

    public bool IsSecure => Scheme == "sips";

    public System.Net.IPEndPoint? HostEndPoint
    {
        get
        {
            if (System.Net.IPAddress.TryParse(Host, out var address))
            {
                var port = Port > 0 ? Port : 5060;
                return new System.Net.IPEndPoint(address, port);
            }
            return null;
        }
    }

    public static bool TryParse(string? uriString, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out SipUri? uri)
    {
        try
        {
            uri = Parse(uriString ?? string.Empty);
            return true;
        }
        catch (Exception e) when (e is SipParseException or FormatException or ArgumentException or OverflowException)
        {
            // Only swallow parse-related exceptions that are expected during URI parsing (Drongo-6bj).
            // Fatal CLR exceptions (OutOfMemoryException, StackOverflowException, etc.) are NOT caught
            // and will propagate to the caller as intended.
            uri = null;
            return false;
        }
    }

    public static SipUri CreateAor(string user, string domain) => new("sip", domain, user: user);
}

public class SipParseException : Exception
{
    public SipParseException(string message) : base(message) { }
}
