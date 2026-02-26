namespace Drongo.Core.Prack;

/// <summary>
/// Generates RSeq and Rack header values per RFC 3262.
/// </summary>
public class RSeqRackHeaderGenerator
{
    /// <summary>
    /// Generates an RSeq header value from a sequence number.
    /// </summary>
    /// <param name="sequenceNumber">The sequence number</param>
    /// <returns>The RSeq header value (e.g., "123")</returns>
    public string GenerateRSeqHeader(int sequenceNumber)
    {
        return sequenceNumber.ToString();
    }

    /// <summary>
    /// Generates a Rack header value from RackInfo.
    /// Format: RSeq Method CSeq (e.g., "123 INVITE 456")
    /// </summary>
    /// <param name="rackInfo">The RackInfo containing RSeq, method, and CSeq</param>
    /// <returns>The Rack header value</returns>
    public string GenerateRackHeader(RackInfo rackInfo)
    {
        return $"{rackInfo.RSeq} {rackInfo.Method} {rackInfo.CSeq}";
    }
}
