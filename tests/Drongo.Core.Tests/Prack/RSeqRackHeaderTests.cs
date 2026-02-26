using Drongo.Core.Prack;
using Xunit;

namespace Drongo.Core.Tests.Prack;

/// <summary>
/// Tests for RSeq and Rack header parsing and generation per RFC 3262.
/// </summary>
public class RSeqRackHeaderTests
{
    [Fact]
    public void ParseRSeqHeader_WithValidSequenceNumber_ReturnsSequenceNumber()
    {
        // RED: Test parses RSeq header value "123" and returns 123
        var parser = new RSeqRackHeaderParser();
        
        var result = parser.ParseRSeqHeader("123");
        
        Assert.Equal(123, result);
    }

    [Fact]
    public void ParseRSeqHeader_WithInvalidFormat_ThrowsFormatException()
    {
        // RED: Test rejects non-numeric RSeq header
        var parser = new RSeqRackHeaderParser();
        
        Assert.Throws<FormatException>(() => parser.ParseRSeqHeader("abc"));
    }

    [Fact]
    public void ParseRSeqHeader_WithEmptyString_ThrowsFormatException()
    {
        // RED: Test rejects empty RSeq header
        var parser = new RSeqRackHeaderParser();
        
        Assert.Throws<FormatException>(() => parser.ParseRSeqHeader(""));
    }

    [Fact]
    public void ParseRackHeader_WithValidFormat_ReturnsRackInfo()
    {
        // RED: Test parses Rack header "123 INVITE 456" and returns RackInfo
        var parser = new RSeqRackHeaderParser();
        
        var result = parser.ParseRackHeader("123 INVITE 456");
        
        Assert.NotNull(result);
        Assert.Equal(123, result.RSeq);
        Assert.Equal("INVITE", result.Method);
        Assert.Equal(456, result.CSeq);
    }

    [Fact]
    public void ParseRackHeader_WithInvalidFormat_ThrowsFormatException()
    {
        // RED: Test rejects malformed Rack header
        var parser = new RSeqRackHeaderParser();
        
        Assert.Throws<FormatException>(() => parser.ParseRackHeader("123 INVITE"));
    }

    [Fact]
    public void ParseRackHeader_WithNonNumericRSeq_ThrowsFormatException()
    {
        // RED: Test rejects non-numeric RSeq in Rack header
        var parser = new RSeqRackHeaderParser();
        
        Assert.Throws<FormatException>(() => parser.ParseRackHeader("abc INVITE 456"));
    }

    [Fact]
    public void ParseRackHeader_WithNonNumericCSeq_ThrowsFormatException()
    {
        // RED: Test rejects non-numeric CSeq in Rack header
        var parser = new RSeqRackHeaderParser();
        
        Assert.Throws<FormatException>(() => parser.ParseRackHeader("123 INVITE abc"));
    }

    [Fact]
    public void GenerateRSeqHeader_WithSequenceNumber_ReturnsFormattedValue()
    {
        // RED: Test generates RSeq header value from sequence number
        var generator = new RSeqRackHeaderGenerator();
        
        var result = generator.GenerateRSeqHeader(123);
        
        Assert.Equal("123", result);
    }

    [Fact]
    public void GenerateRSeqHeader_WithZeroSequence_ReturnsZero()
    {
        // RED: Test generates RSeq header with sequence 0
        var generator = new RSeqRackHeaderGenerator();
        
        var result = generator.GenerateRSeqHeader(0);
        
        Assert.Equal("0", result);
    }

    [Fact]
    public void GenerateRSeqHeader_WithMaxSequence_ReturnsMaxValue()
    {
        // RED: Test generates RSeq header with large sequence number
        var generator = new RSeqRackHeaderGenerator();
        
        var result = generator.GenerateRSeqHeader(2147483647);
        
        Assert.Equal("2147483647", result);
    }

    [Fact]
    public void GenerateRackHeader_WithRackInfo_ReturnsFormattedValue()
    {
        // RED: Test generates Rack header value from RackInfo
        var generator = new RSeqRackHeaderGenerator();
        var rackInfo = new RackInfo { RSeq = 123, Method = "INVITE", CSeq = 456 };
        
        var result = generator.GenerateRackHeader(rackInfo);
        
        Assert.Equal("123 INVITE 456", result);
    }

    [Fact]
    public void GenerateRackHeader_WithDifferentMethod_ReturnsCorrectMethod()
    {
        // RED: Test generates Rack header with different SIP method
        var generator = new RSeqRackHeaderGenerator();
        var rackInfo = new RackInfo { RSeq = 100, Method = "BYE", CSeq = 200 };
        
        var result = generator.GenerateRackHeader(rackInfo);
        
        Assert.Equal("100 BYE 200", result);
    }
}
