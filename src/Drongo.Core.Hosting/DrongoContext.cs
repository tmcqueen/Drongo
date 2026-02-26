using System.Net;
using Drongo.Core.SIP.Dialogs;
using Drongo.Core.SIP.Messages;
using Drongo.Core.SIP.Registration;

namespace Drongo.Core.Hosting;

public abstract class DrongoContext
{
    public required SipRequest Request { get; init; }
    public required IPEndPoint RemoteEndpoint { get; init; }
    public IDialog? Dialog { get; set; }
    public Dictionary<string, object> Items { get; } = new();
}

public sealed class InviteContext : DrongoContext
{
    public required IInviteRouter Router { get; init; }
    public SipResponse? Response { get; set; }

    public async Task SendResponseAsync(int statusCode, string reasonPhrase)
    {
        Response = new SipResponse(
            statusCode,
            reasonPhrase,
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = Request.Via,
                ["From"] = Request.From,
                ["To"] = Request.To,
                ["Call-ID"] = Request.CallId,
                ["CSeq"] = Request.CSeq
            });
        
        await Router.RouteAsync(this);
    }
}

public sealed class RegisterContext : DrongoContext
{
    public required IRegisterRouter Router { get; init; }
    public required IRegistrar Registrar { get; init; }
    public IReadOnlyList<ContactBinding>? Bindings { get; set; }
    public SipResponse? Response { get; set; }

    public async Task SendResponseAsync(int statusCode, string reasonPhrase)
    {
        var headers = new Dictionary<string, string>
        {
            ["Via"] = Request.Via,
            ["From"] = Request.From,
            ["To"] = Request.To,
            ["Call-ID"] = Request.CallId,
            ["CSeq"] = Request.CSeq
        };

        if (Bindings != null)
        {
            headers["Contact"] = string.Join(", ", Bindings.Select(b => b.ToString()));
            headers["Expires"] = "3600";
        }

        Response = new SipResponse(statusCode, reasonPhrase, "SIP/2.0", headers);
        
        await Router.RouteAsync(this);
    }
}
