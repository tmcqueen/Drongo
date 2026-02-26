using Drongo.Core.Transport;
using System.Net;
using Xunit;

namespace Drongo.Core.Tests.Transport;

/// <summary>
/// Tests for SIP message framing over TCP.
/// Block 2: Detect message boundaries via Content-Length, handle pipelined requests, partial messages.
/// </summary>
public class TcpFramingTests
{
    [Fact]
    public async Task ExtractMessage_ShouldParseSingleCompleteMessage()
    {
        // Arrange
        var framing = new TcpFraming();
        var message = "INVITE sip:bob@example.com SIP/2.0\r\n"
                    + "Via: SIP/2.0/TCP pc33.example.com;branch=z9hG4bK776asdhds\r\n"
                    + "Content-Length: 0\r\n"
                    + "\r\n";
        
        // Act
        var data = System.Text.Encoding.UTF8.GetBytes(message);
        framing.BufferData(data);
        var extracted = framing.ExtractMessage();
        
        // Assert
        Assert.NotNull(extracted);
        var extractedStr = System.Text.Encoding.UTF8.GetString(extracted);
        Assert.Equal(message, extractedStr);
    }

    [Fact]
    public async Task ExtractMessage_ShouldHandlePartialMessage()
    {
        // Arrange
        var framing = new TcpFraming();
        
        // Manually create message with known lengths
        var headerLine1 = "INVITE sip:bob@example.com SIP/2.0\r\n";
        var headerLine2 = "Content-Length: 4\r\n";
        var headerEnd = "\r\n";
        var body = "BODY";
        var message = headerLine1 + headerLine2 + headerEnd + body;
        
        var headerBytes = System.Text.Encoding.UTF8.GetBytes(headerLine1 + headerLine2 + headerEnd);
        var bodyBytes = System.Text.Encoding.UTF8.GetBytes(body);
        
        // Buffer only headers and 1 byte of body (incomplete)
        var partial = new byte[headerBytes.Length + 1];
        Array.Copy(headerBytes, partial, headerBytes.Length);
        partial[headerBytes.Length] = bodyBytes[0];
        
        framing.BufferData(partial);
        
        // Act - should return null (incomplete body)
        var extracted = framing.ExtractMessage();
        
        // Assert
        Assert.Null(extracted);
        
        // Buffer the remaining body bytes
        var remaining = new byte[bodyBytes.Length - 1];
        Array.Copy(bodyBytes, 1, remaining, 0, bodyBytes.Length - 1);
        framing.BufferData(remaining);
        
        // Now should extract complete message
        extracted = framing.ExtractMessage();
        Assert.NotNull(extracted);
        var extractedStr = System.Text.Encoding.UTF8.GetString(extracted);
        Assert.Equal(message, extractedStr);
    }

    [Fact]
    public async Task ExtractMessage_ShouldHandlePipelinedMessages()
    {
        // Arrange
        var framing = new TcpFraming();
        var msg1 = "INVITE sip:bob@example.com SIP/2.0\r\n"
                 + "Via: SIP/2.0/TCP pc33.example.com;branch=z9hG4bK1\r\n"
                 + "Content-Length: 0\r\n"
                 + "\r\n";
        var msg2 = "INVITE sip:alice@example.com SIP/2.0\r\n"
                 + "Via: SIP/2.0/TCP pc33.example.com;branch=z9hG4bK2\r\n"
                 + "Content-Length: 0\r\n"
                 + "\r\n";
        
        var pipelined = msg1 + msg2;
        framing.BufferData(System.Text.Encoding.UTF8.GetBytes(pipelined));
        
        // Act - extract first message
        var extracted1 = framing.ExtractMessage();
        
        // Assert
        Assert.NotNull(extracted1);
        var extracted1Str = System.Text.Encoding.UTF8.GetString(extracted1);
        Assert.Equal(msg1, extracted1Str);
        
        // Extract second message
        var extracted2 = framing.ExtractMessage();
        Assert.NotNull(extracted2);
        var extracted2Str = System.Text.Encoding.UTF8.GetString(extracted2);
        Assert.Equal(msg2, extracted2Str);
        
        // No more messages
        var extracted3 = framing.ExtractMessage();
        Assert.Null(extracted3);
    }

    [Fact]
    public async Task ExtractMessage_ShouldHandleMessageWithBody()
    {
        // Arrange
        var framing = new TcpFraming();
        var body = "v=0\r\no=user1 53655765 2353687637 IN IP4 128.3.4.5\r\n";
        var message = "INVITE sip:bob@example.com SIP/2.0\r\n"
                    + "Via: SIP/2.0/TCP pc33.example.com\r\n"
                    + "Content-Length: " + body.Length.ToString() + "\r\n"
                    + "\r\n"
                    + body;
        
        // Act
        framing.BufferData(System.Text.Encoding.UTF8.GetBytes(message));
        var extracted = framing.ExtractMessage();
        
        // Assert
        Assert.NotNull(extracted);
        var extractedStr = System.Text.Encoding.UTF8.GetString(extracted);
        Assert.Equal(message, extractedStr);
    }

    [Fact]
    public async Task ExtractMessage_ShouldHandleMultipleHeadersWithContentLength()
    {
        // Arrange
        var framing = new TcpFraming();
        var body = "test body";
        var message = "SIP/2.0 200 OK\r\n"
                    + "Via: SIP/2.0/TCP pc33.example.com;branch=z9hG4bK776\r\n"
                    + "To: Alice <sip:alice@example.com>\r\n"
                    + "From: Bob <sip:bob@example.com>;tag=1928301774\r\n"
                    + "Call-ID: a84b4c76e66710@pc33.example.com\r\n"
                    + "CSeq: 314159 INVITE\r\n"
                    + "Content-Length: " + body.Length.ToString() + "\r\n"
                    + "Content-Type: text/plain\r\n"
                    + "\r\n"
                    + body;
        
        // Act
        framing.BufferData(System.Text.Encoding.UTF8.GetBytes(message));
        var extracted = framing.ExtractMessage();
        
        // Assert
        Assert.NotNull(extracted);
        var extractedStr = System.Text.Encoding.UTF8.GetString(extracted);
        Assert.Equal(message, extractedStr);
    }

    [Fact]
    public async Task ExtractMessage_ShouldHandleZeroContentLength()
    {
        // Arrange
        var framing = new TcpFraming();
        var message = "200 OK\r\n"
                    + "Via: SIP/2.0/TCP pc33.example.com\r\n"
                    + "Content-Length: 0\r\n"
                    + "\r\n";
        
        // Act
        framing.BufferData(System.Text.Encoding.UTF8.GetBytes(message));
        var extracted = framing.ExtractMessage();
        
        // Assert
        Assert.NotNull(extracted);
        var extractedStr = System.Text.Encoding.UTF8.GetString(extracted);
        Assert.Equal(message, extractedStr);
    }

    [Fact]
    public async Task ExtractMessage_ShouldPreserveBufferAfterExtraction()
    {
        // Arrange
        var framing = new TcpFraming();
        var msg1 = "INVITE sip:bob@example.com SIP/2.0\r\n"
                 + "Via: SIP/2.0/TCP pc33.example.com\r\n"
                 + "Content-Length: 0\r\n"
                 + "\r\n";
        var msg2 = "INVITE sip:alice@example.com SIP/2.0\r\n"
                 + "Via: SIP/2.0/TCP pc33.example.com\r\n"
                 + "Content-Length: 0\r\n"
                 + "\r\n";
        
        var data = System.Text.Encoding.UTF8.GetBytes(msg1 + msg2);
        
        // Act
        framing.BufferData(data);
        var extracted1 = framing.ExtractMessage();
        
        // Verify buffer still has second message
        var extracted2 = framing.ExtractMessage();
        
        // Assert
        Assert.NotNull(extracted1);
        Assert.NotNull(extracted2);
    }
}
