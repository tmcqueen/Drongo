namespace Drongo.Core.Messages;

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
        var result = $"{Scheme}:";

        if (!string.IsNullOrEmpty(User))
            result += $"{User}@";

        result += Host;

        if (Port > 0)
            result += $":{Port}";

        if (!string.IsNullOrEmpty(Parameters))
            result += $";{Parameters}";

        if (!string.IsNullOrEmpty(Headers))
            result += $"?{Headers}";

        return result;
    }

    public bool IsSecure => Scheme == "sips";

    public static SipUri CreateAor(string user, string domain) => new("sip", domain, user: user);
}

public class SipParseException : Exception
{
    public SipParseException(string message) : base(message) { }
}
