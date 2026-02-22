using Drongo.Core.Messages;
using Shouldly;
using Xunit;

namespace Drongo.Core.Tests.Messages;

public class SipUriTests
{
    // ------------------------------------------------------------------ //
    // TryParse — happy-path
    // ------------------------------------------------------------------ //

    [Fact]
    public void TryParse_ValidSipUri_ReturnsTrueAndPopulatesUri()
    {
        var result = SipUri.TryParse("sip:bob@biloxi.com", out var uri);

        result.ShouldBeTrue();
        uri.ShouldNotBeNull();
        uri!.User.ShouldBe("bob");
        uri.Host.ShouldBe("biloxi.com");
        uri.Scheme.ShouldBe("sip");
    }

    [Fact]
    public void TryParse_ValidSipsUri_ReturnsTrueAndSetsIsSecure()
    {
        var result = SipUri.TryParse("sips:alice@atlanta.com:5061", out var uri);

        result.ShouldBeTrue();
        uri.ShouldNotBeNull();
        uri!.IsSecure.ShouldBeTrue();
        uri.Port.ShouldBe(5061);
    }

    [Fact]
    public void TryParse_UriWithParameters_ReturnsTrueAndParsesParameters()
    {
        var result = SipUri.TryParse("sip:bob@biloxi.com;transport=udp", out var uri);

        result.ShouldBeTrue();
        uri.ShouldNotBeNull();
        uri!.Parameters.ShouldBe("transport=udp");
    }

    [Fact]
    public void TryParse_UriWithHeaders_ReturnsTrueAndParsesHeaders()
    {
        var result = SipUri.TryParse("sip:bob@biloxi.com?subject=project", out var uri);

        result.ShouldBeTrue();
        uri.ShouldNotBeNull();
        uri!.Headers.ShouldBe("subject=project");
    }

    [Fact]
    public void TryParse_UriWithAngleBrackets_ReturnsTrueAndStripsAngleBrackets()
    {
        var result = SipUri.TryParse("<sip:alice@atlanta.com>", out var uri);

        result.ShouldBeTrue();
        uri.ShouldNotBeNull();
        uri!.User.ShouldBe("alice");
        uri.Host.ShouldBe("atlanta.com");
    }

    // ------------------------------------------------------------------ //
    // TryParse — malformed inputs that must return false, not throw
    // ------------------------------------------------------------------ //

    [Fact]
    public void TryParse_NullInput_ReturnsFalseWithNullUri()
    {
        var result = SipUri.TryParse(null, out var uri);

        result.ShouldBeFalse();
        uri.ShouldBeNull();
    }

    [Fact]
    public void TryParse_EmptyString_ReturnsFalseWithNullUri()
    {
        var result = SipUri.TryParse(string.Empty, out var uri);

        result.ShouldBeFalse();
        uri.ShouldBeNull();
    }

    [Fact]
    public void TryParse_WhitespaceOnly_ReturnsFalseWithNullUri()
    {
        var result = SipUri.TryParse("   ", out var uri);

        result.ShouldBeFalse();
        uri.ShouldBeNull();
    }

    [Fact]
    public void TryParse_MissingScheme_ReturnsFalseWithNullUri()
    {
        // No colon at all → schemeEnd < 0
        var result = SipUri.TryParse("bob@biloxi.com", out var uri);

        result.ShouldBeFalse();
        uri.ShouldBeNull();
    }

    [Fact]
    public void TryParse_UnsupportedScheme_ReturnsFalseWithNullUri()
    {
        var result = SipUri.TryParse("http://biloxi.com", out var uri);

        result.ShouldBeFalse();
        uri.ShouldBeNull();
    }

    [Fact]
    public void TryParse_InvalidPort_ReturnsFalseWithNullUri()
    {
        var result = SipUri.TryParse("sip:bob@biloxi.com:notaport", out var uri);

        result.ShouldBeFalse();
        uri.ShouldBeNull();
    }

    [Fact]
    public void TryParse_MalformedInput_DoesNotThrow()
    {
        // Regression: bare catch{} was swallowing fatal exceptions;
        // narrowed catch must still not escape parse-related exceptions.
        var exception = Record.Exception(() => SipUri.TryParse(":::invalid:::", out _));

        exception.ShouldBeNull();
    }

    [Fact]
    public void TryParse_RandomGarbage_ReturnsFalseWithNullUri()
    {
        var result = SipUri.TryParse("not a URI at all !!!", out var uri);

        result.ShouldBeFalse();
        uri.ShouldBeNull();
    }

    // ------------------------------------------------------------------ //
    // ToString — correctness and format
    // ------------------------------------------------------------------ //

    [Fact]
    public void ToString_MinimalUri_ReturnsSchemeColonHost()
    {
        var uri = new SipUri("sip", "biloxi.com");

        uri.ToString().ShouldBe("sip:biloxi.com");
    }

    [Fact]
    public void ToString_UriWithUser_IncludesUserAt()
    {
        var uri = new SipUri("sip", "biloxi.com", user: "bob");

        uri.ToString().ShouldBe("sip:bob@biloxi.com");
    }

    [Fact]
    public void ToString_UriWithPort_IncludesColonPort()
    {
        var uri = new SipUri("sip", "biloxi.com", port: 5061);

        uri.ToString().ShouldBe("sip:biloxi.com:5061");
    }

    [Fact]
    public void ToString_UriWithParameters_AppendsSemicolonParameters()
    {
        var uri = new SipUri("sip", "biloxi.com", parameters: "transport=udp");

        uri.ToString().ShouldBe("sip:biloxi.com;transport=udp");
    }

    [Fact]
    public void ToString_UriWithHeaders_AppendsQuestionMarkHeaders()
    {
        var uri = new SipUri("sip", "biloxi.com", headers: "subject=project");

        uri.ToString().ShouldBe("sip:biloxi.com?subject=project");
    }

    [Fact]
    public void ToString_FullUri_ProducesCorrectlyConcatenatedString()
    {
        var uri = new SipUri("sip", "biloxi.com", port: 5060, user: "bob",
            parameters: "transport=tcp", headers: "subject=project");

        uri.ToString().ShouldBe("sip:bob@biloxi.com:5060;transport=tcp?subject=project");
    }

    [Fact]
    public void ToString_RoundTrip_ParsedUriEqualsOriginalString()
    {
        const string original = "sip:alice@atlanta.com:5060;transport=udp";
        var uri = SipUri.Parse(original);

        uri.ToString().ShouldBe(original);
    }

    [Fact]
    public void ToString_SecureUri_StartsWithSips()
    {
        var uri = new SipUri("sips", "atlanta.com");

        uri.ToString().ShouldStartWith("sips:");
    }

    // ------------------------------------------------------------------ //
    // Zero-port guard — Port == 0 must not appear in output
    // ------------------------------------------------------------------ //

    [Fact]
    public void ToString_ZeroPort_DoesNotIncludePortInOutput()
    {
        var uri = new SipUri("sip", "biloxi.com", port: 0);

        // The scheme colon is expected; there must be no host:port colon
        uri.ToString().ShouldBe("sip:biloxi.com");
    }
}
