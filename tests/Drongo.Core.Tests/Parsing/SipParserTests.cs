using System.Buffers;
using Drongo.Core.Parsing;
using Drongo.Core.Messages;
using Xunit;

namespace Drongo.Core.Tests.Parsing;

public class SipParserTests
{
    private readonly SipParser _parser = new();

    [Fact]
    public void ParseRequest_ValidInvite_ReturnsSuccess()
    {
        var data = "INVITE sip:bob@biloxi.com SIP/2.0\r\n" +
                   "Via: SIP/2.0/UDP pc33.atlanta.com;branch=z9hG4bK776asdhds\r\n" +
                   "Max-Forwards: 70\r\n" +
                   "To: Bob <sip:bob@biloxi.com>\r\n" +
                   "From: Alice <sip:alice@atlanta.com>;tag=1928301774\r\n" +
                   "Call-ID: a84b4c76e66710@pc33.atlanta.com\r\n" +
                   "CSeq: 314159 INVITE\r\n" +
                   "Contact: <sip:alice@pc33.atlanta.com>\r\n" +
                   "Content-Length: 0\r\n" +
                   "\r\n";

        var result = _parser.ParseRequest(new ReadOnlySequence<byte>(System.Text.Encoding.ASCII.GetBytes(data)));

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.NotNull(result.Request);
        Assert.Equal(SipMethod.Invite, result.Request.Method);
        Assert.Equal("bob", result.Request.RequestUri.User);
        Assert.Equal("biloxi.com", result.Request.RequestUri.Host);
        Assert.Equal("a84b4c76e66710@pc33.atlanta.com", result.Request.CallId);
    }

    [Fact]
    public void ParseRequest_InvalidMethod_ReturnsFailure()
    {
        var data = "INVALID sip:bob@biloxi.com SIP/2.0\r\n" +
                   "Via: SIP/2.0/UDP pc33.atlanta.com;branch=z9hG4bK776asdhds\r\n" +
                   "To: Bob <sip:bob@biloxi.com>\r\n" +
                   "From: Alice <sip:alice@atlanta.com>;tag=1928301774\r\n" +
                   "Call-ID: test\r\n" +
                   "CSeq: 1 INVALID\r\n" +
                   "Content-Length: 0\r\n" +
                   "\r\n";

        var result = _parser.ParseRequest(new ReadOnlySequence<byte>(System.Text.Encoding.ASCII.GetBytes(data)));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ParseResponse_Valid200Ok_ReturnsSuccess()
    {
        var data = "SIP/2.0 200 OK\r\n" +
                   "Via: SIP/2.0/UDP pc33.atlanta.com;branch=z9hG4bK776asdhds\r\n" +
                   "To: Bob <sip:bob@biloxi.com>;tag=a6c85cf\r\n" +
                   "From: Alice <sip:alice@atlanta.com>;tag=1928301774\r\n" +
                   "Call-ID: a84b4c76e66710@pc33.atlanta.com\r\n" +
                   "CSeq: 314159 INVITE\r\n" +
                   "Contact: <sip:bob@192.0.2.4>\r\n" +
                   "Content-Length: 0\r\n" +
                   "\r\n";

        var result = _parser.ParseResponse(new ReadOnlySequence<byte>(System.Text.Encoding.ASCII.GetBytes(data)));

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.NotNull(result.Response);
        Assert.Equal(200, result.Response.StatusCode);
        Assert.StartsWith("OK", result.Response.ReasonPhrase);
    }

    [Fact]
    public void ParseResponse_InvalidStatusCode_ReturnsFailure()
    {
        var data = "SIP/2.0 NOTANUMBER OK\r\n" +
                   "Via: SIP/2.0/UDP pc33.atlanta.com;branch=z9hG4bK776asdhds\r\n" +
                   "To: Bob <sip:bob@biloxi.com>\r\n" +
                   "From: Alice <sip:alice@atlanta.com>\r\n" +
                   "Call-ID: test\r\n" +
                   "CSeq: 1 INVITE\r\n" +
                   "Content-Length: 0\r\n" +
                   "\r\n";

        var result = _parser.ParseResponse(new ReadOnlySequence<byte>(System.Text.Encoding.ASCII.GetBytes(data)));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ParseRequest_WithBody_ParsesCorrectly()
    {
        var sdpBody = "v=0\r\n" +
                      "o=alice 2890844526 2890844526 IN IP4 pc33.atlanta.com\r\n";
        
        var data = "INVITE sip:bob@biloxi.com SIP/2.0\r\n" +
                   "Via: SIP/2.0/UDP pc33.atlanta.com;branch=z9hG4bK776asdhds\r\n" +
                   "To: Bob <sip:bob@biloxi.com>\r\n" +
                   "From: Alice <sip:alice@atlanta.com>;tag=1928301774\r\n" +
                   "Call-ID: test\r\n" +
                   "CSeq: 1 INVITE\r\n" +
                   "Content-Type: application/sdp\r\n" +
                   "Content-Length: " + sdpBody.Length + "\r\n" +
                   "\r\n" +
                   sdpBody;

        var result = _parser.ParseRequest(new ReadOnlySequence<byte>(System.Text.Encoding.ASCII.GetBytes(data)));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Request);
    }

    [Fact]
    public void ParseRequest_MissingRequiredHeaders_ReturnsSuccessWithHeaders()
    {
        var data = "INVITE sip:bob@biloxi.com SIP/2.0\r\n" +
                   "Content-Length: 0\r\n" +
                   "\r\n";

        var result = _parser.ParseRequest(new ReadOnlySequence<byte>(System.Text.Encoding.ASCII.GetBytes(data)));

        Assert.True(result.IsSuccess);
    }
}
