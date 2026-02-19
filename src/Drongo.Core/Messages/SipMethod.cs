namespace Drongo.Core.Messages;

public enum SipMethod
{
    Invite,
    Ack,
    Bye,
    Cancel,
    Options,
    Register,
    Notify,
    Subscribe,
    Info,
    Prack,
    Update,
    Refer,
    Message,
    Publish,
}

public static class SipMethodExtensions
{
    public static string ToMethodString(this SipMethod method) => method switch
    {
        SipMethod.Invite => "INVITE",
        SipMethod.Ack => "ACK",
        SipMethod.Bye => "BYE",
        SipMethod.Cancel => "CANCEL",
        SipMethod.Options => "OPTIONS",
        SipMethod.Register => "REGISTER",
        SipMethod.Notify => "NOTIFY",
        SipMethod.Subscribe => "SUBSCRIBE",
        SipMethod.Info => "INFO",
        SipMethod.Prack => "PRACK",
        SipMethod.Update => "UPDATE",
        SipMethod.Refer => "REFER",
        SipMethod.Message => "MESSAGE",
        SipMethod.Publish => "PUBLISH",
        _ => method.ToString().ToUpperInvariant()
    };

    public static SipMethod ParseMethod(string method) => method.ToUpperInvariant() switch
    {
        "INVITE" => SipMethod.Invite,
        "ACK" => SipMethod.Ack,
        "BYE" => SipMethod.Bye,
        "CANCEL" => SipMethod.Cancel,
        "OPTIONS" => SipMethod.Options,
        "REGISTER" => SipMethod.Register,
        "NOTIFY" => SipMethod.Notify,
        "SUBSCRIBE" => SipMethod.Subscribe,
        "INFO" => SipMethod.Info,
        "PRACK" => SipMethod.Prack,
        "UPDATE" => SipMethod.Update,
        "REFER" => SipMethod.Refer,
        "MESSAGE" => SipMethod.Message,
        "PUBLISH" => SipMethod.Publish,
        _ => throw new SipParseException($"Unknown SIP method: {method}")
    };
}
