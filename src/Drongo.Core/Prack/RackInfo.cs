namespace Drongo.Core.Prack;

/// <summary>
/// Represents information extracted from a Rack header per RFC 3262.
/// </summary>
public class RackInfo
{
    /// <summary>
    /// The RSeq number from the provisional response being acknowledged.
    /// </summary>
    public int RSeq { get; set; }

    /// <summary>
    /// The SIP method of the request that generated the provisional response.
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// The CSeq number of the request that generated the provisional response.
    /// </summary>
    public int CSeq { get; set; }
}
