using System.Text;

namespace Drongo.Core.Transport;

/// <summary>
/// Handles SIP message framing for TCP transport.
/// Block 2: Detect message boundaries via Content-Length, handle pipelined requests, partial messages.
/// Buffers incoming data and extracts complete SIP messages based on Content-Length header.
/// </summary>
public class TcpFraming
{
    private byte[] _buffer = Array.Empty<byte>();
    private int _bufferLength;

    /// <summary>
    /// Buffers incoming data.
    /// </summary>
    public void BufferData(byte[] data)
    {
        if (data == null || data.Length == 0)
            return;

        // Expand buffer if needed
        int requiredCapacity = _bufferLength + data.Length;
        if (_buffer.Length < requiredCapacity)
        {
            Array.Resize(ref _buffer, Math.Max(requiredCapacity, _buffer.Length * 2));
        }

        // Append new data
        Array.Copy(data, 0, _buffer, _bufferLength, data.Length);
        _bufferLength += data.Length;
    }

    /// <summary>
    /// Extracts a complete SIP message from the buffer.
    /// Returns null if no complete message is available.
    /// Removes the extracted message from the buffer.
    /// </summary>
    public byte[]? ExtractMessage()
    {
        if (_bufferLength == 0)
            return null;

        // Find end of headers (double CRLF)
        int headerEndPos = FindHeaderEnd();
        if (headerEndPos == -1)
            return null;

        // Extract headers
        var headerBytes = new byte[headerEndPos];
        Array.Copy(_buffer, headerBytes, headerEndPos);
        var headerStr = Encoding.UTF8.GetString(headerBytes);

        // Parse Content-Length from headers
        int contentLength = ExtractContentLength(headerStr);
        if (contentLength == -1)
            return null; // No Content-Length found

        // Message body starts after the double CRLF
        int bodyStart = headerEndPos;
        int messageEnd = bodyStart + contentLength;

        // Check if we have the complete body
        if (_bufferLength < messageEnd)
            return null; // Incomplete message

        // Extract complete message (headers + body)
        var message = new byte[messageEnd];
        Array.Copy(_buffer, message, messageEnd);

        // Remove message from buffer
        int remaining = _bufferLength - messageEnd;
        if (remaining > 0)
        {
            Array.Copy(_buffer, messageEnd, _buffer, 0, remaining);
        }
        _bufferLength = remaining;

        return message;
    }

    /// <summary>
    /// Finds the position after the double CRLF that ends the headers.
    /// Returns -1 if headers are incomplete.
    /// </summary>
    private int FindHeaderEnd()
    {
        // Look for \r\n\r\n
        for (int i = 0; i < _bufferLength - 3; i++)
        {
            if (_buffer[i] == '\r' && _buffer[i + 1] == '\n' &&
                _buffer[i + 2] == '\r' && _buffer[i + 3] == '\n')
            {
                return i + 4; // Position after the double CRLF
            }
        }
        return -1;
    }

    /// <summary>
    /// Extracts the Content-Length value from SIP headers.
    /// Returns -1 if not found.
    /// </summary>
    private int ExtractContentLength(string headers)
    {
        // RFC 3261: Content-Length header (case-insensitive)
        // Can appear as "Content-Length:" or "l:" (compact form)
        var lines = headers.Split(new[] { "\r\n" }, StringSplitOptions.None);
        
        foreach (var line in lines)
        {
            if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
            {
                if (line.Length > 15)
                {
                    var value = line.Substring(15).Trim();
                    if (int.TryParse(value, out int length))
                        return length;
                }
            }
            else if (line.StartsWith("l:", StringComparison.OrdinalIgnoreCase))
            {
                // Compact form of Content-Length
                if (line.Length > 2)
                {
                    var value = line.Substring(2).Trim();
                    if (int.TryParse(value, out int length))
                        return length;
                }
            }
        }

        return -1;
    }
}
