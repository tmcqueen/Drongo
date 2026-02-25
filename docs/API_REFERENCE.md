# Block 2 API Reference: CallLeg & CallLegOrchestrator

## CallLeg Class

The `CallLeg` class represents one side of a B2BUA dialog, tracking state, sequence numbers, and dialog participants per RFC3261 Section 12.

### Constructor

```csharp
public CallLeg(
    string callId,
    string localTag,
    SipUri localUri,
    SipUri remoteUri,
    bool isSecure,
    ILogger<CallLeg> logger)
```

**Purpose**: Create a new dialog leg for one participant

**Parameters**:
- `callId` - The shared SIP Call-ID for this dialog (required, non-empty)
- `localTag` - This leg's unique tag identifier (required, non-empty)
- `localUri` - This leg's SIP URI (required, non-null)
- `remoteUri` - Remote peer's SIP URI (required, non-null)
- `isSecure` - Whether this leg uses secure transport (TLS)
- `logger` - Logging instance (required, non-null)

**Initialization**:
- State: `Initial`
- LocalSequenceNumber: `1`
- RemoteSequenceNumber: `0`
- RemoteTag: `null` (established later via HandleResponse or internal setter)

**Throws**:
- `ArgumentException` - if `callId` or `localTag` is null or empty
- `ArgumentNullException` - if `localUri`, `remoteUri`, or `logger` is null

**Example**:
```csharp
var leg = new CallLeg(
    callId: "call-123@example.com",
    localTag: "tag-1",
    localUri: new SipUri("sip", "alice@example.com", 5060),
    remoteUri: new SipUri("sip", "bob@example.com", 5060),
    isSecure: false,
    logger: loggerFactory.CreateLogger<CallLeg>());
```

---

### Properties

#### State
```csharp
public CallLegState State { get; }
```

**Purpose**: Get the current RFC3261 dialog state

**Values**: `Initial`, `Inviting`, `ProvisionalResponse`, `Confirmed`, `Failed`, `Terminating`, `Terminated`

**Thread-Safety**: ⚠️ Read-only. Mutations are not thread-safe when concurrent calls invoke `HandleResponse()` or `TransitionToState()`.

#### CallId
```csharp
public string CallId { get; }
```

**Purpose**: Get the shared SIP Call-ID for this dialog

**Invariant**: Never changes after construction. Identical for both UAC and UAS legs of the same dialog.

**Usage**: Use to look up dialogs in the CallLegOrchestrator dictionary.

#### LocalTag
```csharp
public string LocalTag { get; }
```

**Purpose**: Get this leg's unique tag identifier

**Invariant**: Never changes after construction. Unique within the Call-ID namespace.

**RFC3261**: Part of dialog identity: `(Call-ID, LocalTag, RemoteTag)`

#### RemoteTag
```csharp
public string? RemoteTag { get; internal set; }
```

**Purpose**: Get the remote peer's tag identifier

**Initialization**: `null` initially; established during dialog creation (usually from first response)

**Invariant**: Immutable after establishment (restricted to internal set within Drongo.Core)

**RFC3261**: Part of dialog identity. Must never change for dialog lifetime.

**Access Control**: `internal set` prevents external code from corrupting dialog identity.

#### LocalUri
```csharp
public SipUri LocalUri { get; }
```

**Purpose**: Get this leg's SIP URI (address of record)

**RFC3261**: From `From` header (UAC perspective) or `To` header (UAS perspective)

#### RemoteUri
```csharp
public SipUri RemoteUri { get; }
```

**Purpose**: Get the remote peer's SIP URI

**RFC3261**: From `To` header (UAC perspective) or `From` header (UAS perspective)

#### IsSecure
```csharp
public bool IsSecure { get; }
```

**Purpose**: Indicate whether this leg uses secure transport (SIPS/TLS)

**Usage**: Determines whether responses are validated against secure URIs

#### LocalSequenceNumber
```csharp
public long LocalSequenceNumber { get; }
```

**Purpose**: Get the sequence number (CSeq) for the next request sent by this leg

**Initial Value**: `1`

**RFC3261**: The CSeq value for INVITE or other requests. Incremented by `GetNextSequenceNumber()`.

**Type**: `long` (64-bit) to prevent overflow in long-running calls

#### RemoteSequenceNumber
```csharp
public long RemoteSequenceNumber { get; }
```

**Purpose**: Get the last sequence number received from the remote peer

**Initial Value**: `0`

**RFC3261**: Validated against CSeq in received requests to detect retransmissions and out-of-order messages

**Type**: `long` (must match LocalSequenceNumber for consistency)

---

### Methods

#### HandleResponse
```csharp
public void HandleResponse(SipResponse response)
```

**Purpose**: Process an incoming SIP response and update dialog state

**Parameters**:
- `response` - The SIP response to process (required, non-null)

**State Machine**:
- `1xx` responses → `ProvisionalResponse` (early dialog)
- `2xx` responses → `Confirmed` (dialog established)
- `3xx`-`6xx` responses → `Failed` (dialog failed)
- Unknown status codes → no state change (but logged)

**Invalid Transitions**:
- Late `1xx` after `2xx`: State remains `Confirmed`, warning logged
- Error response while `Confirmed`: State remains `Confirmed`, warning logged

**RFC3261**: Implements Section 12.1 response handling and state transitions

**Throws**: None (uses logging for invalid transitions)

**Example**:
```csharp
var response = new SipResponse(200, "OK", "SIP/2.0", new Dictionary<string, string>
{
    ["Call-ID"] = "call-123@example.com",
    ["CSeq"] = "1 INVITE"
});

leg.HandleResponse(response);  // State → Confirmed
```

#### HandleRequest
```csharp
public void HandleRequest(SipRequest request)
```

**Purpose**: Process an incoming SIP request (current: logging only)

**Parameters**:
- `request` - The SIP request to process (required, non-null)

**Current Implementation**: Logs request method and local tag; no state machine changes.

**⚠️ TODO**: This method is a stub. Full implementation needed for:
- CSeq validation and sequence number updates
- Contact header parsing
- Route set establishment
- Request target validation

**RFC3261**: Section 12.2 defines in-dialog request processing

**Throws**: None (currently)

#### GetNextSequenceNumber
```csharp
public long GetNextSequenceNumber()
```

**Purpose**: Increment and return the next CSeq value for sending a request

**Return Value**: The new sequence number (incremented from previous)

**Behavior**: Increments `LocalSequenceNumber` and returns the new value

**Thread-Safety**: ⚠️ NOT thread-safe. Use within a message-sending lock.

**Example**:
```csharp
var cseq = leg.GetNextSequenceNumber();  // Returns 2
var cseq2 = leg.GetNextSequenceNumber(); // Returns 3
```

#### IsEstablished
```csharp
public bool IsEstablished()
```

**Purpose**: Check if the dialog is confirmed and operational

**Return Value**: `true` if `State == CallLegState.Confirmed`, `false` otherwise

**Usage**: Check before sending in-dialog requests (ACK, BYE, etc.)

**Example**:
```csharp
if (leg.IsEstablished())
{
    // Safe to send BYE to terminate dialog
    await SendBye(leg);
}
```

#### UpdateRemoteSequenceNumber
```csharp
internal void UpdateRemoteSequenceNumber(long sequenceNumber)
```

**Purpose**: Update the last received sequence number from the remote peer

**Parameters**:
- `sequenceNumber` - CSeq value from received request

**Behavior**: Updates `RemoteSequenceNumber` only if the new value is greater than the current

**RFC3261**: Used to detect retransmissions and validate in-dialog requests

**Access**: Internal only (called by transaction layer)

**Example**:
```csharp
// After receiving INVITE with CSeq: 1 INVITE
leg.UpdateRemoteSequenceNumber(1);  // remoteSeqNum = 1

// Duplicate INVITE with same CSeq
leg.UpdateRemoteSequenceNumber(1);  // No change (1 > 1 is false)

// New request with CSeq: 2 BYE
leg.UpdateRemoteSequenceNumber(2);  // remoteSeqNum = 2
```

#### TransitionToState
```csharp
internal void TransitionToState(CallLegState newState)
```

**Purpose**: Explicitly transition the leg to a new state (e.g., for BYE processing)

**Parameters**:
- `newState` - The desired target state

**Validation**: Enforces valid forward transitions per RFC3261

**Access**: Internal only (called by CallLegOrchestrator)

**Throws**:
- `InvalidOperationException` - if transition is backward or invalid
  - Message: `"Invalid state transition from {Current} to {Target} on leg {LocalTag}"`

**Example**:
```csharp
// Valid: Confirmed → Terminating (sending BYE)
leg.TransitionToState(CallLegState.Terminating);  // ✓ Succeeds

// Invalid: Confirmed → ProvisionalResponse (backward)
leg.TransitionToState(CallLegState.ProvisionalResponse);  // ✗ Throws
```

---

## CallLegOrchestrator Class

The `CallLegOrchestrator` manages pairs of call legs and routes messages between them, enforcing B2BUA invariants.

### Constructor

```csharp
public CallLegOrchestrator(ILogger<CallLegOrchestrator> logger)
```

**Purpose**: Create a new B2BUA orchestrator

**Parameters**:
- `logger` - Logging instance (required, non-null)

**Internal State**:
- `_dialogLegs`: ConcurrentDictionary of `(CallId → (UAC, UAS))` leg pairs

**Example**:
```csharp
var orchestrator = new CallLegOrchestrator(loggerFactory.CreateLogger<CallLegOrchestrator>());
```

---

### Methods

#### CreateCallLegPair
```csharp
public (ICallLeg uacLeg, ICallLeg uasLeg) CreateCallLegPair(
    string callId,
    string uacTag,
    string uasTag,
    SipUri uacUri,
    SipUri uasUri,
    bool isSecure)
```

**Purpose**: Create a new pair of dialog legs for UAC and UAS participants

**Parameters**:
- `callId` - Shared Call-ID for the dialog (required, non-empty)
- `uacTag` - UAC leg's unique tag (required, non-empty)
- `uasTag` - UAS leg's unique tag (required, non-empty)
- `uacUri` - UAC's SIP URI (required, non-null)
- `uasUri` - UAS's SIP URI (required, non-null)
- `isSecure` - Whether to use secure transport (SIPS/TLS)

**Return Value**: Tuple of `(uacLeg, uasLeg)` interfaces

**Leg Setup**:
- UAC leg: `LocalTag=uacTag`, `RemoteTag=uasTag` (learns peer from UAS)
- UAS leg: `LocalTag=uasTag`, `RemoteTag=uacTag` (learns peer from UAC)

**Mutual State**: Both legs start in `Initial` state

**Throws**:
- `InvalidOperationException` - if a leg pair for this `callId` already exists
  - Message: `"Call leg pair for Call-ID '{callId}' already exists"`
- `ArgumentException` - if `callId`, `uacTag`, or `uasTag` is null or empty
- `ArgumentNullException` - if `uacUri` or `uasUri` is null

**Example**:
```csharp
var (uac, uas) = orchestrator.CreateCallLegPair(
    callId: "call-123@example.com",
    uacTag: "caller-tag",
    uasTag: "callee-tag",
    uacUri: new SipUri("sip", "alice@example.com", 5060),
    uasUri: new SipUri("sip", "bob@example.com", 5060),
    isSecure: false);

// Both legs now exist and can receive messages
uac.State.ShouldBe(CallLegState.Initial);
uas.State.ShouldBe(CallLegState.Initial);
```

#### TryGetCallLegs
```csharp
public bool TryGetCallLegs(
    string callId,
    out ICallLeg uacLeg,
    out ICallLeg uasLeg)
```

**Purpose**: Retrieve the leg pair for a dialog

**Parameters**:
- `callId` - The dialog's Call-ID (required, non-empty)
- `uacLeg` - Output: the UAC leg (null if not found)
- `uasLeg` - Output: the UAS leg (null if not found)

**Return Value**:
- `true` - if legs were found
- `false` - if no dialog with this Call-ID exists

**Usage**: Check before routing responses or processing in-dialog requests

**Example**:
```csharp
if (orchestrator.TryGetCallLegs(callId, out var uac, out var uas))
{
    // Route response to both legs
    uas.HandleResponse(response);
    uac.HandleResponse(response);
}
else
{
    _logger.LogWarning("Dialog {CallId} not found", callId);
    return null;  // Send 481 Call-Leg Does Not Exist
}
```

#### RouteProvisionalResponse
```csharp
public SipResponse? RouteProvisionalResponse(
    string callId,
    SipResponse response)
```

**Purpose**: Route a provisional (1xx) response between legs

**Parameters**:
- `callId` - The dialog's Call-ID (required, non-empty)
- `response` - The provisional response (required, non-null)

**Response Types**: Handles `100 Trying`, `180 Ringing`, `183 Session Progress`, etc.

**State Update**:
- Both UAC and UAS legs transition to `ProvisionalResponse`
- Late 1xx (after 2xx) logged as warning, state unchanged

**Return Value**:
- The `response` if routing succeeded
- `null` if dialog not found (caller should send 481 response)

**Throws**: None (logs warnings for invalid states)

**RFC3261**: Section 12.1 provisional response handling

**Example**:
```csharp
var response = new SipResponse(183, "Session Progress", ...);
var result = orchestrator.RouteProvisionalResponse(callId, response);

if (result != null)
{
    await SendToUac(result);  // Forward to caller
}
else
{
    await SendError481(callId);  // Dialog not found
}
```

#### RouteFinalResponse
```csharp
public SipResponse? RouteFinalResponse(
    string callId,
    SipResponse response)
```

**Purpose**: Route a final (2xx-6xx) response that establishes or fails the dialog

**Parameters**:
- `callId` - The dialog's Call-ID (required, non-empty)
- `response` - The final response (required, non-null)

**Behavior for 2xx (Confirmation)**:
- Both legs transition to `Confirmed`
- Dialog is now established, can send in-dialog requests
- ACK processing is expected next

**Behavior for 3xx-6xx (Failure)**:
- Both legs transition to `Failed`
- Dialog remains established but not confirmed
- No in-dialog requests allowed

**Return Value**:
- The `response` if routing succeeded
- `null` if dialog not found

**Throws**: None (logs warnings for invalid transitions)

**RFC3261**: Section 12.1 final response handling

**Example**:
```csharp
// 200 OK confirms dialog
var ok = new SipResponse(200, "OK", ...);
orchestrator.RouteFinalResponse(callId, ok);
// Both legs now Confirmed

// Send BYE to terminate
await SendBye(callId);
```

#### RouteErrorResponse
```csharp
public SipResponse? RouteErrorResponse(
    string callId,
    SipResponse response)
```

**Purpose**: Route an error (3xx-6xx) response that fails the dialog

**Parameters**:
- `callId` - The dialog's Call-ID (required, non-empty)
- `response` - The error response (required, non-null)

**Response Types**: Handles `300 Multiple Choices`, `403 Forbidden`, `486 Busy Here`, etc.

**State Update**:
- Both UAC and UAS legs transition to `Failed`
- Dialog remains in memory but is not confirmed
- No further in-dialog requests should be sent

**Return Value**:
- The `response` if routing succeeded
- `null` if dialog not found

**Throws**: None (logs warnings)

**RFC3261**: Section 12.1 error response handling

**Example**:
```csharp
var busy = new SipResponse(486, "Busy Here", ...);
orchestrator.RouteErrorResponse(callId, busy);
// Both legs now Failed
```

---

## State Machine Reference

### Valid State Transitions

```
Initial ──Inviting──> Provisional ──Confirmed──┐
  └─────────────────────────────────────────────┘

    └────────> Failed ◄─────┘

Confirmed ──Terminating──> Terminated
```

### Transition Entry Points

| From State | To State | Trigger | Method |
|-----------|----------|---------|--------|
| Initial | Inviting | Sending INVITE | (Explicit, not in Block 2) |
| Initial/Inviting | ProvisionalResponse | 1xx response | `HandleResponse()` |
| Any Early | Confirmed | 2xx response | `HandleResponse()` |
| Initial/Early | Failed | 3xx-6xx response | `HandleResponse()` |
| Confirmed | Terminating | Sending BYE | `TransitionToState()` |
| Terminating | Terminated | ACK received | `TransitionToState()` |

---

## Exception Reference

### InvalidOperationException

**Source**: `CallLeg.TransitionToState()`, `CallLegOrchestrator.CreateCallLegPair()`

**Scenarios**:
- Backward state transition (e.g., `Confirmed` → `Initial`)
- Duplicate Call-ID in `CreateCallLegPair()`

**Handling**:
```csharp
try
{
    leg.TransitionToState(CallLegState.Initial);  // ← Backward!
}
catch (InvalidOperationException ex)
{
    _logger.LogError(ex, "State machine violation");
    // Cleanup and tear down call
}
```

### ArgumentException / ArgumentNullException

**Source**: Constructors and methods with validation

**Scenarios**:
- Null or empty string parameters
- Null object parameters
- Duplicate Call-ID

**Handling**:
```csharp
try
{
    var leg = new CallLeg(null, "tag", uri1, uri2, false, logger);
}
catch (ArgumentException ex)
{
    _logger.LogError(ex, "Invalid parameters");
}
```

---

## Threading and Concurrency

### Current Guarantees

✅ **Safe**:
- `CreateCallLegPair()` - atomic insertion via ConcurrentDictionary
- `TryGetCallLegs()` - thread-safe lookup

⚠️ **Not Safe**:
- `HandleResponse()` on concurrent calls - no per-leg locking
- Sequence number updates - non-atomic increment

### Recommendations

- **Per-Request Processing**: Assume single message handler per dialog
- **I/O-Based Concurrency**: Use async/await, not shared-state threads
- **Future**: Add lock-based synchronization per leg for true concurrent access

---

## Logging Levels

### DEBUG
- State transitions (successful)
- Message routing
- Request/response processing

### INFORMATION
- Dialog confirmation (2xx received)
- New call leg pair creation

### WARNING
- Invalid state transitions (late responses)
- Missing dialogs for routing

### ERROR
- Exceptions during message processing

---

**Document Version**: 1.0
**Last Updated**: 2026-02-25
**Author**: Claude (Block 2 API)
**Status**: Complete (Documents r1-r4 implementations)
