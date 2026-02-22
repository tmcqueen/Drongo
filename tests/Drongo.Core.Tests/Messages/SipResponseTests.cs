using Drongo.Core.Messages;
using Shouldly;
using Xunit;

namespace Drongo.Core.Tests.Messages;

public class SipResponseTests
{
    private static readonly IReadOnlyDictionary<string, string> BaseHeaders = new Dictionary<string, string>
    {
        ["Via"] = "SIP/2.0/UDP pc33.atlanta.com;branch=z9hG4bK776asdhds",
        ["From"] = "Alice <sip:alice@atlanta.com>;tag=1928301774",
        ["To"] = "Bob <sip:bob@biloxi.com>;tag=a6c85cf",
        ["Call-ID"] = "test-call-id",
        ["CSeq"] = "1 INVITE"
    };

    private static SipResponse CreateOkResponse() => new(200, "OK", "SIP/2.0", BaseHeaders);

    // ── Value equality (record) ──────────────────────────────────────────────

    [Fact]
    public void Equality_TwoInstancesWithSameValues_AreEqual()
    {
        // Arrange
        var a = new SipResponse(200, "OK", "SIP/2.0", new Dictionary<string, string>(BaseHeaders));
        var b = new SipResponse(200, "OK", "SIP/2.0", new Dictionary<string, string>(BaseHeaders));

        // Act & Assert
        (a == b).ShouldBeTrue();
        a.Equals(b).ShouldBeTrue();
    }

    [Fact]
    public void Equality_TwoInstancesWithDifferentStatusCode_AreNotEqual()
    {
        // Arrange
        var a = new SipResponse(200, "OK", "SIP/2.0", new Dictionary<string, string>(BaseHeaders));
        var b = new SipResponse(404, "Not Found", "SIP/2.0", new Dictionary<string, string>(BaseHeaders));

        // Act & Assert
        (a == b).ShouldBeFalse();
        a.Equals(b).ShouldBeFalse();
    }

    [Fact]
    public void GetHashCode_TwoInstancesWithSameValues_ProduceSameHash()
    {
        // Arrange
        var headers = new Dictionary<string, string>(BaseHeaders);
        var a = new SipResponse(200, "OK", "SIP/2.0", headers);
        var b = new SipResponse(200, "OK", "SIP/2.0", headers);

        // Act & Assert
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    // ── with-expression ──────────────────────────────────────────────────────

    [Fact]
    public void WithExpression_ChangeStatusCode_ReturnsNewInstanceWithUpdatedStatusCode()
    {
        // Arrange
        var original = CreateOkResponse();

        // Act
        var modified = original with { StatusCode = 486, ReasonPhrase = "Busy Here" };

        // Assert
        modified.StatusCode.ShouldBe(486);
        modified.ReasonPhrase.ShouldBe("Busy Here");
        modified.SipVersion.ShouldBe(original.SipVersion);
        original.StatusCode.ShouldBe(200); // original unchanged
    }

    [Fact]
    public void WithExpression_ChangeReasonPhrase_OriginalIsUnchanged()
    {
        // Arrange
        var original = CreateOkResponse();

        // Act
        var modified = original with { ReasonPhrase = "Success" };

        // Assert
        modified.ReasonPhrase.ShouldBe("Success");
        original.ReasonPhrase.ShouldBe("OK");
    }

    // ── Status classification computed properties ─────────────────────────────

    [Theory]
    [InlineData(100)]
    [InlineData(180)]
    [InlineData(199)]
    public void IsProvisional_1xxResponse_ReturnsTrue(int statusCode)
    {
        var response = new SipResponse(statusCode, "Provisional", "SIP/2.0", BaseHeaders);

        response.IsProvisional.ShouldBeTrue();
    }

    [Theory]
    [InlineData(200)]
    [InlineData(299)]
    public void IsSuccess_2xxResponse_ReturnsTrue(int statusCode)
    {
        var response = new SipResponse(statusCode, "Success", "SIP/2.0", BaseHeaders);

        response.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(300)]
    [InlineData(399)]
    public void IsRedirection_3xxResponse_ReturnsTrue(int statusCode)
    {
        var response = new SipResponse(statusCode, "Redirect", "SIP/2.0", BaseHeaders);

        response.IsRedirection.ShouldBeTrue();
    }

    [Theory]
    [InlineData(400)]
    [InlineData(499)]
    public void IsClientError_4xxResponse_ReturnsTrue(int statusCode)
    {
        var response = new SipResponse(statusCode, "Client Error", "SIP/2.0", BaseHeaders);

        response.IsClientError.ShouldBeTrue();
    }

    [Theory]
    [InlineData(500)]
    [InlineData(599)]
    public void IsServerError_5xxResponse_ReturnsTrue(int statusCode)
    {
        var response = new SipResponse(statusCode, "Server Error", "SIP/2.0", BaseHeaders);

        response.IsServerError.ShouldBeTrue();
    }

    [Theory]
    [InlineData(600)]
    [InlineData(699)]
    public void IsGlobalError_6xxResponse_ReturnsTrue(int statusCode)
    {
        var response = new SipResponse(statusCode, "Global Error", "SIP/2.0", BaseHeaders);

        response.IsGlobalError.ShouldBeTrue();
    }

    [Fact]
    public void IsProvisional_2xxResponse_ReturnsFalse()
    {
        var response = CreateOkResponse();

        response.IsProvisional.ShouldBeFalse();
    }

    // ── Computed header properties ────────────────────────────────────────────

    [Fact]
    public void CallId_ReturnsValueFromHeaders()
    {
        var response = CreateOkResponse();

        response.CallId.ShouldBe("test-call-id");
    }

    [Fact]
    public void From_ReturnsValueFromHeaders()
    {
        var response = CreateOkResponse();

        response.From.ShouldBe("Alice <sip:alice@atlanta.com>;tag=1928301774");
    }

    [Fact]
    public void To_ReturnsValueFromHeaders()
    {
        var response = CreateOkResponse();

        response.To.ShouldBe("Bob <sip:bob@biloxi.com>;tag=a6c85cf");
    }

    [Fact]
    public void Contact_WhenAbsent_ReturnsNull()
    {
        var response = CreateOkResponse();

        response.Contact.ShouldBeNull();
    }

    [Fact]
    public void Contact_WhenPresent_ReturnsValue()
    {
        var headers = new Dictionary<string, string>(BaseHeaders) { ["Contact"] = "<sip:bob@biloxi.com>" };
        var response = new SipResponse(200, "OK", "SIP/2.0", headers);

        response.Contact.ShouldBe("<sip:bob@biloxi.com>");
    }

    [Fact]
    public void HasBody_WhenNoBody_ReturnsFalse()
    {
        var response = CreateOkResponse();

        response.HasBody.ShouldBeFalse();
    }

    [Fact]
    public void HasBody_WhenBodyPresent_ReturnsTrue()
    {
        var body = new ReadOnlyMemory<byte>(System.Text.Encoding.ASCII.GetBytes("v=0\r\n"));
        var response = new SipResponse(200, "OK", "SIP/2.0", BaseHeaders, body);

        response.HasBody.ShouldBeTrue();
    }

    [Fact]
    public void ContentLength_WhenPresent_ReturnsInt()
    {
        var headers = new Dictionary<string, string>(BaseHeaders) { ["Content-Length"] = "100" };
        var response = new SipResponse(200, "OK", "SIP/2.0", headers);

        response.ContentLength.ShouldBe(100);
    }

    [Fact]
    public void ContentLength_WhenAbsent_ReturnsNull()
    {
        var response = CreateOkResponse();

        response.ContentLength.ShouldBeNull();
    }

    // ── Static factory methods ────────────────────────────────────────────────

    [Fact]
    public void Create_WithStatusCodeAndReason_CreatesResponse()
    {
        var response = SipResponse.Create(200, "OK", BaseHeaders);

        response.StatusCode.ShouldBe(200);
        response.ReasonPhrase.ShouldBe("OK");
        response.SipVersion.ShouldBe("SIP/2.0");
    }

    [Fact]
    public void CreateTrying_ReturnsProvisionalResponse()
    {
        var response = SipResponse.CreateTrying(BaseHeaders);

        response.StatusCode.ShouldBe(100);
        response.ReasonPhrase.ShouldBe("Trying");
        response.IsProvisional.ShouldBeTrue();
    }

    [Fact]
    public void CreateRinging_ReturnsRingingResponse()
    {
        var response = SipResponse.CreateRinging(BaseHeaders);

        response.StatusCode.ShouldBe(180);
        response.ReasonPhrase.ShouldBe("Ringing");
    }

    [Fact]
    public void CreateOk_Returns200Response()
    {
        var response = SipResponse.CreateOk(BaseHeaders);

        response.StatusCode.ShouldBe(200);
        response.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void CreateBusyHere_Returns486Response()
    {
        var response = SipResponse.CreateBusyHere(BaseHeaders);

        response.StatusCode.ShouldBe(486);
        response.ReasonPhrase.ShouldBe("Busy Here");
        response.IsClientError.ShouldBeTrue();
    }

    [Fact]
    public void CreateNotFound_Returns404Response()
    {
        var response = SipResponse.CreateNotFound(BaseHeaders);

        response.StatusCode.ShouldBe(404);
        response.ReasonPhrase.ShouldBe("Not Found");
    }

    [Fact]
    public void CreateBadRequest_Returns400Response()
    {
        var response = SipResponse.CreateBadRequest("Missing required header", BaseHeaders);

        response.StatusCode.ShouldBe(400);
        response.ReasonPhrase.ShouldBe("Missing required header");
    }

    [Fact]
    public void CreateServerError_Returns500Response()
    {
        var response = SipResponse.CreateServerError(BaseHeaders);

        response.StatusCode.ShouldBe(500);
        response.IsServerError.ShouldBeTrue();
    }

    // ── Helper methods ────────────────────────────────────────────────────────

    [Fact]
    public void GetHeader_MissingHeader_ThrowsInvalidOperationException()
    {
        var response = CreateOkResponse();

        Should.Throw<InvalidOperationException>(() => response.GetHeader("X-Custom"));
    }

    [Fact]
    public void TryGetHeader_MissingHeader_ReturnsNull()
    {
        var response = CreateOkResponse();

        response.TryGetHeader("X-Custom").ShouldBeNull();
    }

    [Fact]
    public void HasHeader_ExistingHeader_ReturnsTrue()
    {
        var response = CreateOkResponse();

        response.HasHeader("Via").ShouldBeTrue();
    }

    [Fact]
    public void HasHeader_MissingHeader_ReturnsFalse()
    {
        var response = CreateOkResponse();

        response.HasHeader("X-Custom").ShouldBeFalse();
    }

    [Fact]
    public void ToString_ReturnsStartLineWithStatusCode()
    {
        var response = CreateOkResponse();

        var result = response.ToString();

        result.ShouldStartWith("SIP/2.0 200 OK");
    }

    [Fact]
    public void Constructor_NullHeaders_DefaultsToEmptyDictionary()
    {
        var response = new SipResponse(200, "OK", "SIP/2.0", null!);

        response.Headers.ShouldNotBeNull();
        response.Headers.Count.ShouldBe(0);
    }
}
