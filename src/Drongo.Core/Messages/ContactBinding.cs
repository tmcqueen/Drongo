namespace Drongo.Core.Messages;

public sealed record ContactBinding
{
    public SipUri ContactUri { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public string? InstanceId { get; init; }
    public float? QValue { get; init; }
    public string? Methods { get; init; }

    public ContactBinding(SipUri contactUri, DateTimeOffset expiresAt, string? instanceId = null, float? qValue = null, string? methods = null)
    {
        ContactUri = contactUri;
        ExpiresAt = expiresAt;
        InstanceId = instanceId;
        QValue = qValue;
        Methods = methods;
    }

    public static ContactBinding Parse(string contactHeader)
    {
        var uriEnd = contactHeader.IndexOf(';');
        var uriString = uriEnd >= 0 ? contactHeader[..uriEnd] : contactHeader;
        
        var uri = SipUri.Parse(uriString.Trim('<', '>', ' '));

        var expiresAt = DateTimeOffset.MaxValue;
        string? instanceId = null;
        float? qValue = null;
        string? methods = null;

        if (uriEnd >= 0)
        {
            var parameters = contactHeader[(uriEnd + 1)..];
            foreach (var param in parameters.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var keyValue = param.Split('=', 2);
                var key = keyValue[0].Trim().ToLowerInvariant();
                var value = keyValue.Length > 1 ? keyValue[1].Trim() : null;

                switch (key)
                {
                    case "expires" when value != null && int.TryParse(value, out var exp):
                        expiresAt = DateTimeOffset.UtcNow.AddSeconds(exp);
                        break;
                    case "q" when value != null && float.TryParse(value, out var q):
                        qValue = q;
                        break;
                    case "instance" when value != null:
                        instanceId = value.Trim('"');
                        break;
                    case "methods" when value != null:
                        methods = value.Trim('"');
                        break;
                }
            }
        }

        return new ContactBinding(uri, expiresAt, instanceId, qValue, methods);
    }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    public override string ToString()
    {
        var result = $"<{ContactUri}>";
        
        if (QValue.HasValue)
            result += $";q={QValue}";
        
        if (ExpiresAt != DateTimeOffset.MaxValue)
            result += $";expires={(int)(ExpiresAt - DateTimeOffset.UtcNow).TotalSeconds}";
        
        if (!string.IsNullOrEmpty(InstanceId))
            result += $";instance=\"{InstanceId}\"";
        
        if (!string.IsNullOrEmpty(Methods))
            result += $";methods=\"{Methods}\"";
        
        return result;
    }
}
