using Drongo.Core.Messages;
using Shouldly;
using Xunit;

namespace Drongo.Core.Tests.Messages;

public class SipRequestTests
{
    private static readonly IReadOnlyDictionary<string, string> BaseHeaders = new Dictionary<string, string>
    {
        ["Via"] = "SIP/2.0/UDP pc33.atlanta.com;branch=z9hG4bK776asdhds",
        ["From"] = "Alice <sip:alice@atlanta.com>;tag=1928301774",
        ["To"] = "Bob <sip:bob@biloxi.com>",
        ["Call-ID"] = "test-call-id",
        ["CSeq"] = "1 INVITE",
        ["Max-Forwards"] = "70"
    };

    private static SipRequest CreateInviteRequest() => new(
        SipMethod.Invite,
        new SipUri("sip", "bob@biloxi.com"),
        "SIP/2.0",
        BaseHeaders);

    // ── Value equality (record) ──────────────────────────────────────────────

    [Fact]
    public void Equality_TwoInstancesWithSameValues_AreEqual()
    {
        // Arrange
        var a = new SipRequest(
            SipMethod.Invite,
            new SipUri("sip", "bob@biloxi.com"),
            "SIP/2.0",
            new Dictionary<string, string>(BaseHeaders));

        var b = new SipRequest(
            SipMethod.Invite,
            new SipUri("sip", "bob@biloxi.com"),
            "SIP/2.0",
            new Dictionary<string, string>(BaseHeaders));

        // Act & Assert
        (a == b).ShouldBeTrue();
        a.Equals(b).ShouldBeTrue();
    }

    [Fact]
    public void Equality_TwoInstancesWithDifferentMethod_AreNotEqual()
    {
        // Arrange
        var a = new SipRequest(SipMethod.Invite, new SipUri("sip", "bob@biloxi.com"), "SIP/2.0", new Dictionary<string, string>(BaseHeaders));
        var b = new SipRequest(SipMethod.Bye, new SipUri("sip", "bob@biloxi.com"), "SIP/2.0", new Dictionary<string, string>(BaseHeaders));

        // Act & Assert
        (a == b).ShouldBeFalse();
        a.Equals(b).ShouldBeFalse();
    }

    [Fact]
    public void GetHashCode_TwoInstancesWithSameValues_ProduceSameHash()
    {
        // Arrange
        var headers = new Dictionary<string, string>(BaseHeaders);
        var a = new SipRequest(SipMethod.Invite, new SipUri("sip", "bob@biloxi.com"), "SIP/2.0", headers);
        var b = new SipRequest(SipMethod.Invite, new SipUri("sip", "bob@biloxi.com"), "SIP/2.0", headers);

        // Act & Assert
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    // ── with-expression ──────────────────────────────────────────────────────

    [Fact]
    public void WithExpression_ChangeMethod_ReturnsNewInstanceWithUpdatedMethod()
    {
        // Arrange
        var original = CreateInviteRequest();

        // Act
        var modified = original with { Method = SipMethod.Bye };

        // Assert
        modified.Method.ShouldBe(SipMethod.Bye);
        modified.RequestUri.ShouldBe(original.RequestUri);
        modified.SipVersion.ShouldBe(original.SipVersion);
        modified.Headers.ShouldBe(original.Headers);
        original.Method.ShouldBe(SipMethod.Invite); // original unchanged
    }

    [Fact]
    public void WithExpression_ChangeSipVersion_OriginalIsUnchanged()
    {
        // Arrange
        var original = CreateInviteRequest();

        // Act
        var modified = original with { SipVersion = "SIP/3.0" };

        // Assert
        modified.SipVersion.ShouldBe("SIP/3.0");
        original.SipVersion.ShouldBe("SIP/2.0");
    }

    // ── Computed properties still work ───────────────────────────────────────

    [Fact]
    public void CallId_ReturnsValueFromHeaders()
    {
        var request = CreateInviteRequest();

        request.CallId.ShouldBe("test-call-id");
    }

    [Fact]
    public void From_ReturnsValueFromHeaders()
    {
        var request = CreateInviteRequest();

        request.From.ShouldBe("Alice <sip:alice@atlanta.com>;tag=1928301774");
    }

    [Fact]
    public void To_ReturnsValueFromHeaders()
    {
        var request = CreateInviteRequest();

        request.To.ShouldBe("Bob <sip:bob@biloxi.com>");
    }

    [Fact]
    public void CSeq_ReturnsValueFromHeaders()
    {
        var request = CreateInviteRequest();

        request.CSeq.ShouldBe("1 INVITE");
    }

    [Fact]
    public void Via_ReturnsValueFromHeaders()
    {
        var request = CreateInviteRequest();

        request.Via.ShouldBe("SIP/2.0/UDP pc33.atlanta.com;branch=z9hG4bK776asdhds");
    }

    [Fact]
    public void Contact_WhenAbsent_ReturnsNull()
    {
        var request = CreateInviteRequest();

        request.Contact.ShouldBeNull();
    }

    [Fact]
    public void Contact_WhenPresent_ReturnsValue()
    {
        var headers = new Dictionary<string, string>(BaseHeaders)
        {
            ["Contact"] = "<sip:alice@atlanta.com>"
        };
        var request = new SipRequest(SipMethod.Invite, new SipUri("sip", "bob@biloxi.com"), "SIP/2.0", headers);

        request.Contact.ShouldBe("<sip:alice@atlanta.com>");
    }

    [Fact]
    public void HasBody_WhenNoBody_ReturnsFalse()
    {
        var request = CreateInviteRequest();

        request.HasBody.ShouldBeFalse();
    }

    [Fact]
    public void HasBody_WhenBodyPresent_ReturnsTrue()
    {
        var body = new ReadOnlyMemory<byte>(System.Text.Encoding.ASCII.GetBytes("v=0\r\n"));
        var request = new SipRequest(SipMethod.Invite, new SipUri("sip", "bob@biloxi.com"), "SIP/2.0", new Dictionary<string, string>(BaseHeaders), body);

        request.HasBody.ShouldBeTrue();
    }

    [Fact]
    public void ContentLength_WhenPresent_ReturnsInt()
    {
        var headers = new Dictionary<string, string>(BaseHeaders)
        {
            ["Content-Length"] = "42"
        };
        var request = new SipRequest(SipMethod.Invite, new SipUri("sip", "bob@biloxi.com"), "SIP/2.0", headers);

        request.ContentLength.ShouldBe(42);
    }

    [Fact]
    public void ContentLength_WhenAbsent_ReturnsNull()
    {
        var request = CreateInviteRequest();

        request.ContentLength.ShouldBeNull();
    }

    [Fact]
    public void GetHeader_MissingHeader_ThrowsInvalidOperationException()
    {
        var request = CreateInviteRequest();

        Should.Throw<InvalidOperationException>(() => request.GetHeader("X-Custom"));
    }

    [Fact]
    public void TryGetHeader_MissingHeader_ReturnsNull()
    {
        var request = CreateInviteRequest();

        request.TryGetHeader("X-Custom").ShouldBeNull();
    }

    [Fact]
    public void HasHeader_ExistingHeader_ReturnsTrue()
    {
        var request = CreateInviteRequest();

        request.HasHeader("Via").ShouldBeTrue();
    }

    [Fact]
    public void HasHeader_MissingHeader_ReturnsFalse()
    {
        var request = CreateInviteRequest();

        request.HasHeader("X-Custom").ShouldBeFalse();
    }

    [Fact]
    public void ToString_ReturnsStartLineWithMethod()
    {
        var request = CreateInviteRequest();

        var result = request.ToString();

        result.ShouldStartWith("INVITE sip:bob@biloxi.com SIP/2.0");
    }

    [Fact]
    public void Constructor_NullHeaders_DefaultsToEmptyDictionary()
    {
        var request = new SipRequest(SipMethod.Invite, new SipUri("sip", "bob@biloxi.com"), "SIP/2.0", null!);

        request.Headers.ShouldNotBeNull();
        request.Headers.Count.ShouldBe(0);
    }
}
