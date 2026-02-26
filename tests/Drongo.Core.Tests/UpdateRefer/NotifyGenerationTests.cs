using Drongo.Core.Messages;
using Xunit;

namespace Drongo.Core.Tests.UpdateRefer;

/// <summary>
/// Tests for NOTIFY request generation per RFC 3515.
/// </summary>
public class NotifyGenerationTests
{
    [Fact]
    public void SipMethod_NotifyMethod_IsSupported()
    {
        // RED: Test that NOTIFY method is defined
        Assert.Equal(SipMethod.Notify, SipMethodExtensions.ParseMethod("NOTIFY"));
    }

    [Fact]
    public void SipMethod_ReferMethod_IsSupported()
    {
        // RED: Test that REFER method is defined
        Assert.Equal(SipMethod.Refer, SipMethodExtensions.ParseMethod("REFER"));
    }

    [Fact]
    public void SipMethod_UpdateMethod_IsSupported()
    {
        // RED: Test that UPDATE method is defined
        Assert.Equal(SipMethod.Update, SipMethodExtensions.ParseMethod("UPDATE"));
    }

    [Fact]
    public void GenerateNotifyRequest_WithTransferStatus_IncludesEventHeader()
    {
        // RED: Test NOTIFY request includes Event header for transfer
        var request = new SipRequest(
            SipMethod.Notify,
            new SipUri("sip", "caller.example.com"),
            "SIP/2.0",
            new Dictionary<string, string>
            {
                { "Event", "refer" },
                { "Subscription-State", "active" },
                { "Call-ID", "call-123" },
                { "From", "<sip:caller@example.com>;tag=abc" },
                { "To", "<sip:callee@example.com>;tag=xyz" }
            }
        );

        Assert.Equal("refer", request.Headers["Event"]);
    }

    [Fact]
    public void GenerateNotifyRequest_IncludesSubscriptionState()
    {
        // RED: Test NOTIFY includes Subscription-State header
        var request = new SipRequest(
            SipMethod.Notify,
            new SipUri("sip", "caller.example.com"),
            "SIP/2.0",
            new Dictionary<string, string>
            {
                { "Event", "refer" },
                { "Subscription-State", "active" }
            }
        );

        Assert.Equal("active", request.Headers["Subscription-State"]);
    }
}
