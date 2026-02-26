using System;

namespace Drongo.Core.Prack;

/// <summary>
/// Parses RSeq and Rack headers per RFC 3262.
/// </summary>
public class RSeqRackHeaderParser
{
    /// <summary>
    /// Parses an RSeq header value to extract the sequence number.
    /// </summary>
    /// <param name="headerValue">The RSeq header value (e.g., "123")</param>
    /// <returns>The sequence number</returns>
    /// <exception cref="FormatException">If the header value is invalid</exception>
    public int ParseRSeqHeader(string headerValue)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            throw new FormatException("RSeq header value cannot be empty");
        }

        if (!int.TryParse(headerValue.Trim(), out var rseq))
        {
            throw new FormatException($"Invalid RSeq header value: {headerValue}");
        }

        return rseq;
    }

    /// <summary>
    /// Parses a Rack header value to extract RSeq, method, and CSeq.
    /// Format: RSeq Method CSeq (e.g., "123 INVITE 456")
    /// </summary>
    /// <param name="headerValue">The Rack header value</param>
    /// <returns>RackInfo containing parsed values</returns>
    /// <exception cref="FormatException">If the header value is invalid</exception>
    public RackInfo ParseRackHeader(string headerValue)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            throw new FormatException("Rack header value cannot be empty");
        }

        var parts = headerValue.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length != 3)
        {
            throw new FormatException($"Invalid Rack header format. Expected 'RSeq Method CSeq', got: {headerValue}");
        }

        if (!int.TryParse(parts[0], out var rseq))
        {
            throw new FormatException($"Invalid RSeq in Rack header: {parts[0]}");
        }

        var method = parts[1];

        if (!int.TryParse(parts[2], out var cseq))
        {
            throw new FormatException($"Invalid CSeq in Rack header: {parts[2]}");
        }

        return new RackInfo
        {
            RSeq = rseq,
            Method = method,
            CSeq = cseq
        };
    }
}
