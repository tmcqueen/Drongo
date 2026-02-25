# B2BUA (Back-to-Back User Agent) Architecture

## Overview

A Back-to-Back User Agent (B2BUA) is a SIP server that acts as both a User Agent Client (UAC) on one side and a User Agent Server (UAS) on the other side, bridging two call legs into a single logical call.

This document describes the B2BUA architecture implemented in Block 2, the role of `CallLeg` and `CallLegOrchestrator`, and the critical symmetry requirements.

## B2BUA Model

```
┌─────────────────────────────────────────────────┐
│ Caller (Alice)                   Drongo B2BUA   │
│                                                 │
│  INVITE ──────────────────────>  UAC Leg ↓     │
│  (as UAS)        HandleRequest()           │   │
│                                             │   │
│             CallLegOrchestrator             │   │
│             (Routing & State Mgmt)          │   │
│                                             ↓   │
│  ACK        <─────────────────────  Callee (Bob)
│  (as UAC)        HandleResponse()          |   │
│                                           UAS Leg
│                                                 │
└─────────────────────────────────────────────────┘
```

## Components

### CallLeg: Single Dialog Side

Each `CallLeg` represents one participant's view of a dialog:

**Properties**:
- `CallId` - Shared SIP Call-ID (same for both legs)
- `LocalTag` - This leg's unique identifier (different for UAC vs UAS)
- `RemoteTag` - Peer's tag (established during dialog creation)
- `LocalUri` - This leg's endpoint (caller or callee)
- `RemoteUri` - Peer's endpoint
- `State` - RFC3261 dialog state (Initial → Confirmed → Terminated)

**Responsibilities**:
- Track dialog state per RFC3261 Section 12
- Validate state transitions
- Maintain sequence numbers (CSeq)
- Prevent state corruption (no backward transitions)

**Invariants**:
- State only moves forward
- RemoteTag is immutable after establishment
- LocalTag never changes
- CallId identifies the dialog across legs

### CallLegOrchestrator: B2BUA Routing Engine

The `CallLegOrchestrator` manages paired call legs and routes messages between them:

**Responsibilities**:
- Create and maintain UAC/UAS leg pairs
- Route requests from one leg to the other
- Route responses in reverse path
- Maintain consistent state across both legs
- Enforce B2BUA symmetry invariants

**Core Methods**:
```csharp
public (ICallLeg uac, ICallLeg uas) CreateCallLegPair(
    string callId, string uacTag, string uasTag,
    SipUri uacUri, SipUri uasUri, bool isSecure)

public bool TryGetCallLegs(string callId,
    out ICallLeg uacLeg, out ICallLeg uasLeg)

public SipResponse? RouteProvisionalResponse(string callId, SipResponse response)
public SipResponse? RouteFinalResponse(string callId, SipResponse response)
public SipResponse? RouteErrorResponse(string callId, SipResponse response)
```

## Symmetry: The Critical Invariant

### The Problem Without Symmetry

Consider what happens if legs diverge:

```
UAC Leg State: Confirmed (received 2xx)
UAS Leg State: ProvisionalResponse (only 1xx received)

Subsequent BYE arrives at UAS leg...
- UAS leg transitions Provisional → Terminating (invalid! can only go from Confirmed)
- Application logic breaks, calls can't be torn down properly
```

### The Solution: Symmetric Routing

Both legs must process responses identically:

```csharp
// ✅ CORRECT: Symmetric update
public SipResponse? RouteProvisionalResponse(string callId, SipResponse response)
{
    var (uacLeg, uasLeg) = GetLegs(callId);

    // Both legs updated with same response
    uacLeg.HandleResponse(response);  // Both → ProvisionalResponse
    uasLeg.HandleResponse(response);

    return response;
}

// ❌ WRONG: Asymmetric update (pre-fix)
public SipResponse? RouteFinalResponse(string callId, SipResponse response)
{
    var (uacLeg, uasLeg) = GetLegs(callId);

    uasLeg.HandleResponse(response);           // UAS → Confirmed
    uacLeg.TransitionToState(Confirmed);       // UAC → Confirmed (different method!)
    // Different methods = different validation = potential state divergence!
}
```

### Enforcing Symmetry in Tests

Every routing operation must have test coverage verifying **both legs** reach the same state:

```csharp
[Fact]
public void RouteProvisionalResponse_UpdatesBothLegs()
{
    var (uac, uas) = _orchestrator.CreateCallLegPair(...);

    var response = new SipResponse(183, "Session Progress", ...);
    _orchestrator.RouteProvisionalResponse(callId, response);

    // Both legs must be in identical state
    uac.State.ShouldBe(CallLegState.ProvisionalResponse);
    uas.State.ShouldBe(CallLegState.ProvisionalResponse);
}
```

## Dialog Identity

Per RFC3261 Section 12, dialog identity is defined by a 3-tuple:

```
Dialog Identity = (Call-ID, LocalTag, RemoteTag)
```

### Call-ID: Shared Identifier

- **Set by**: Initial INVITE sender (UAC)
- **Used by**: All messages in this call
- **Same for**: Both UAC and UAS legs
- **Invariant**: Never changes for the dialog lifetime

```csharp
// Both legs share the same Call-ID
var (uacLeg, uasLeg) = orchestrator.CreateCallLegPair(
    callId: "call-abc@drongo.local",  // ← Same for both
    uacTag: "tag-1",
    uasTag: "tag-2",
    ...);

uacLeg.CallId == "call-abc@drongo.local"
uasLeg.CallId == "call-abc@drongo.local"  // ✓ Identical
```

### LocalTag and RemoteTag: Directional Identifiers

- **LocalTag**: Generated by this leg, identifies "me"
- **RemoteTag**: Learned from peer, identifies "them"
- **Asymmetric**: UAC's local = UAS's remote, and vice versa

```csharp
// UAC leg perspective
uacLeg.LocalTag   = "tag-1"      // "I am tag-1"
uacLeg.RemoteTag  = "tag-2"      // "Peer is tag-2"

// UAS leg perspective (opposite)
uasLeg.LocalTag   = "tag-2"      // "I am tag-2"
uasLeg.RemoteTag  = "tag-1"      // "Peer is tag-1"

// Dialog identity is the same from both perspectives:
// (Call-ID, LocalTag=tag-1, RemoteTag=tag-2) ← UAC view
// (Call-ID, RemoteTag=tag-1, LocalTag=tag-2) ← UAS view
```

### Why Immutable RemoteTag

The `RemoteTag` is restricted to `internal set` (read-only from outside the assembly) because:

1. **RFC3261 Requirement**: Remote tag is established during dialog creation and never changes
2. **State Corruption Risk**: Modifying remote tag breaks dialog identity
3. **Cross-Leg Consistency**: If one leg's RemoteTag is corrupted, routing fails

```csharp
// ✅ Internal setup during CreateCallLegPair
uacLeg.RemoteTag = uasTag;  // Allowed within Drongo.Core assembly

// ❌ Cannot be set externally (compile error if attempted)
uacLeg.RemoteTag = "different-tag";  // ← Would not compile
```

## Message Flow Example: INVITE → 200 OK

```
Caller (Alice)      Drongo B2BUA      Callee (Bob)
     |                   |                   |
     | INVITE(Call-ID-X) |                   |
     |──────────────────>|                   |
     |                   | CreateCallLegPair |
     |                   | UAC: tag-1, tag-2 |
     |                   | UAS: tag-2, tag-1 |
     |                   |                   |
     |                   | INVITE(Call-ID-X) |
     |                   |──────────────────>|
     |                   |                   |
     |                   | 183 Session Progress
     |                   |<──────────────────|
     | RouteProvisional  |
     | Response          |
     | (both legs: 1xx)  |
     |<──────────────────|
     |                   |
     |                   | 200 OK
     |                   |<──────────────────|
     | RouteFinalResponse|
     | (both legs: 2xx)  |
     |<──────────────────|
     |                   |
     | ACK               |
     |──────────────────>| (routed as request)
     |                   |
     |                   | ACK
     |                   |──────────────────>|
     |                   |
     |◄─── Dialog Confirmed on Both Sides ──────>|
```

### State Progression

| Time | UAC Leg State | UAS Leg State | Event |
|------|--------------|---------------|-------|
| t0 | Initial | Initial | Pair created |
| t1 | Initial → Inviting | Initial | INVITE sent (UAC implicit) |
| t2 | Provisional | Provisional | 183 routed to both |
| t3 | Confirmed | Confirmed | 200 routed to both |
| tn | Terminating | Terminating | BYE processing |
| tn+1 | Terminated | Terminated | Dialog closed |

**Key Observation**: Both legs transition through identical states at approximately the same time (within milliseconds). If one leg misses an update, they diverge.

## Concurrent Access and Thread Safety

### Current State

The implementation uses `ConcurrentDictionary` for thread-safe collection access:

```csharp
private readonly ConcurrentDictionary<string, (ICallLeg uac, ICallLeg uas)> _dialogLegs;

// Safe to call from multiple threads
_dialogLegs.TryGetValue(callId, out var legs);
_dialogLegs.TryAdd(callId, (uac, uas));
```

### Known Limitations

⚠️ **Not Thread-Safe**: Individual leg mutations are NOT protected:

```csharp
// UNSAFE: Race condition possible
var (uacLeg, uasLeg) = _dialogLegs[callId];  // Thread A reads
// Context switch...
// Thread B calls HandleResponse() on same legs
uacLeg.HandleResponse(response);  // Thread A: Concurrent mutation!
```

**Mitigation** (for future work):
- Lock individual legs during response processing
- Use immutable state updates (copy-on-write)
- Queue state transitions through a synchronization primitive

Current implementation assumes single-threaded message processing per dialog (reasonable for typical SIP server architectures with I/O-based concurrency rather than shared-state concurrency).

## Design Decisions & Trade-offs

| Decision | Rationale | Trade-off |
|----------|-----------|-----------|
| Separate CallLeg/Dialog classes | Clarity: specialized vs. generic | Code duplication |
| No abstract base leg interface | YAGNI: ICallLeg sufficient | Harder to mock alternative implementations |
| Immutable RemoteTag | Prevent state corruption | Requires internal setter |
| Symmetric routing always | RFC compliance, prevents bugs | Slight performance overhead (2x leg updates) |
| ConcurrentDictionary only | Simple, fast lookups | Per-leg thread safety not covered |
| State validation on every response | Prevent invalid progressions | Warning log spam on late responses |

## Related Components

- **Dialog Manager** (Block 1): Manages complete dialog lifecycle, complementary to CallLeg
- **Transaction Layer** (Block 1): Handles RFC17 state machines, independent of dialogs
- **Message Parser** (Block 1): Provides SipRequest/SipResponse for routing

## Future Improvements

1. **Consolidate CallLeg and Dialog**: Eliminate duplication, reduce state machine variants
2. **Add Thread Safety**: Per-leg locking or actor model for state mutations
3. **Sequence Number Management**: Move CSeq tracking to Dialog for consistency
4. **Early Dialog Support**: Add support for RFC3262 (PRACK) and early dialog headers
5. **Remote Leg Discovery**: Support B2BUA chains (B2BUA-behind-B2BUA scenarios)

---

**Document Version**: 1.0
**Last Updated**: 2026-02-25
**Author**: Claude (Block 2 Architecture)
**Status**: Complete (Describes current r1-r4 implementation)
