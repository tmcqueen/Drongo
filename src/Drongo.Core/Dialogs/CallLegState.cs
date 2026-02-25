namespace Drongo.Core.Dialogs;

/// <summary>
/// Represents the state of a call leg (either UAC or UAS side of a dialog).
/// Per RFC3261 Section 12, call legs track dialogue state independently.
/// </summary>
public enum CallLegState
{
    /// <summary>Initial state before INVITE sent/received</summary>
    Initial,

    /// <summary>INVITE sent (UAC) or received (UAS), waiting for provisional response</summary>
    Inviting,

    /// <summary>Provisional response (1xx) received, dialog established but incomplete</summary>
    ProvisionalResponse,

    /// <summary>Final response (2xx) received, dialog complete and confirmed</summary>
    Confirmed,

    /// <summary>Rejection response (3xx-6xx) received, dialog failed</summary>
    Failed,

    /// <summary>BYE sent or received, dialog being terminated</summary>
    Terminating,

    /// <summary>Dialog completely terminated</summary>
    Terminated
}
