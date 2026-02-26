using Drongo.Core.Messages;
using Xunit;

namespace Drongo.Core.Tests.UpdateRefer;

/// <summary>
/// Tests for Refer-to header parsing per RFC 3515.
/// </summary>
public class ReferToHeaderTests
{
    [Fact]
    public void ParseReferToHeader_WithValidUri_ReturnsUri()
    {
        // RED: Test parsing Refer-To header with valid SIP URI
        var request = new SipRequest(
            SipMethod.Refer,
            new SipUri("sip", "example.com"),
            "SIP/2.0",
            new Dictionary<string, string>
            {
                { "Refer-To", "<sip:transfer-target@example.com>" }
            }
        );

        var referTo = request.TryGetHeader("Refer-To");
        
        Assert.NotNull(referTo);
        Assert.Contains("sip:transfer-target@example.com", referTo);
    }

    [Fact]
    public void ParseReferToHeader_WithReplacesHeader_ReturnsReplaces()
    {
        // RED: Test parsing Refer-To with Replaces header for transfer
        var request = new SipRequest(
            SipMethod.Refer,
            new SipUri("sip", "example.com"),
            "SIP/2.0",
            new Dictionary<string, string>
            {
                { "Refer-To", "<sip:target@example.com>?Replaces=call-id%3Bto-tag%3Dxyz%3Bfrom-tag%3Dabc" }
            }
        );

        var referTo = request.TryGetHeader("Refer-To");
        
        Assert.NotNull(referTo);
        Assert.Contains("Replaces", referTo);
    }

    [Fact]
    public void HasReferToHeader_WhenPresent_ReturnsTrue()
    {
        // RED: Test checking if Refer-To header exists
        var request = new SipRequest(
            SipMethod.Refer,
            new SipUri("sip", "example.com"),
            "SIP/2.0",
            new Dictionary<string, string>
            {
                { "Refer-To", "<sip:target@example.com>" }
            }
        );

        var hasReferTo = request.HasHeader("Refer-To");
        
        Assert.True(hasReferTo);
    }

    [Fact]
    public void HasReferToHeader_WhenMissing_ReturnsFalse()
    {
        // RED: Test missing Refer-To header returns false
        var request = new SipRequest(
            SipMethod.Refer,
            new SipUri("sip", "example.com"),
            "SIP/2.0",
            new Dictionary<string, string>()
        );

        var hasReferTo = request.HasHeader("Refer-To");
        
        Assert.False(hasReferTo);
    }
}
