# CallLeg State Machine Design

## Overview

The `CallLeg` class implements RFC3261 Section 12 dialog state management for one side of a B2BUA (Back-to-Back User Agent) call. This document describes the state machine design, valid transitions, and invariants.

## State Definitions

The `CallLegState` enum defines seven states representing the lifecycle of a dialog leg:

```
Initial → Inviting → ProvisionalResponse → Confirmed → Terminating → Terminated
                  ↘ Failed (3xx-6xx) ↙
```

### State Descriptions

| State | RFC3261 Term | Purpose | Entry Conditions |
|-------|--------------|---------|------------------|
| `Initial` | Initial (pre-dialog) | Dialog not yet established | Leg creation |
| `Inviting` | Early (no 100rel) | INVITE sent, awaiting response | Explicit transition |
| `ProvisionalResponse` | Early (1xx received) | Received 1xx provisional response | `HandleResponse(1xx)` |
| `Confirmed` | Confirmed | Received 2xx final response | `HandleResponse(2xx)` |
| `Failed` | Terminated (failure) | Received 3xx-6xx error response | `HandleResponse(3xx-6xx)` |
| `Terminating` | Terminated (active) | BYE sent or received | Explicit transition |
| `Terminated` | Terminated (completed) | BYE acknowledged | Explicit transition |

## Valid State Transitions

The state machine enforces forward-only transitions per RFC3261 to prevent state corruption. Invalid transitions (backward, sideways, or out-of-sequence) throw `InvalidOperationException`.

### Transition Rules

```
ENTRY POINT: Initial
├─ Initial → Inviting (explicit: sending INVITE)
├─ Initial → ProvisionalResponse (receiving 1xx directly, rare)
├─ Initial → Confirmed (receiving 2xx directly, rare)
├─ Initial → Failed (receiving 3xx-6xx directly)
│
FROM INVITING:
├─ Inviting → ProvisionalResponse (1xx received)
├─ Inviting → Confirmed (2xx received)
├─ Inviting → Failed (3xx-6xx received)
│
FROM PROVISIONAL RESPONSE:
├─ ProvisionalResponse → Confirmed (2xx received)
├─ ProvisionalResponse → Failed (3xx-6xx received)
├─ ProvisionalResponse → ProvisionalResponse (later 1xx, no-op, allowed)
│
FROM CONFIRMED:
├─ Confirmed → Terminating (BYE sent/received)
├─ Confirmed → Confirmed (late 3xx-6xx, rejected with warning, no state change)
│
FROM TERMINATING:
├─ Terminating → Terminated (BYE acknowledged)
│
FROM FAILED/TERMINATED:
├─ No further transitions (terminal states)
```

### Key Design Decisions

1. **No Backward Transitions**: Once a state is left, it cannot be re-entered
   - Prevents bugs where late responses downgrade state
   - Example: Late 1xx after 2xx will not change state from `Confirmed` to `ProvisionalResponse`

2. **Symmetric Legs**: Both UAC and UAS legs follow identical state progression
   - Enforced by using `HandleResponse()` on both legs in routing
   - Critical for B2BUA correctness (RFC3261 dialog state is mutual)

3. **No-Op Transitions**: Multiple 1xx responses are allowed without state change
   - `ProvisionalResponse → ProvisionalResponse` is valid
   - Prevents exception on receipt of multiple provisional responses

## Implementation in CallLeg.cs

### State Validation

The private static method `IsValidTransition()` uses pattern matching to define legal transitions:

```csharp
private static bool IsValidTransition(CallLegState currentState, CallLegState targetState)
{
    if (currentState == targetState) return true; // No-op always valid

    return (currentState, targetState) switch
    {
        // Initial transitions
        (CallLegState.Initial, CallLegState.Inviting) => true,

        // Early dialog (1xx)
        (CallLegState.Initial, CallLegState.ProvisionalResponse) => true,
        (CallLegState.Inviting, CallLegState.ProvisionalResponse) => true,

        // Confirmation (2xx)
        (CallLegState.Initial, CallLegState.Confirmed) => true,
        (CallLegState.Inviting, CallLegState.Confirmed) => true,
        (CallLegState.ProvisionalResponse, CallLegState.Confirmed) => true,

        // Failure (3xx-6xx)
        (CallLegState.Initial, CallLegState.Failed) => true,
        (CallLegState.Inviting, CallLegState.Failed) => true,
        (CallLegState.ProvisionalResponse, CallLegState.Failed) => true,

        // Termination
        (CallLegState.Confirmed, CallLegState.Terminating) => true,
        (CallLegState.Terminating, CallLegState.Terminated) => true,

        // All others invalid
        _ => false
    };
}
```

### HandleResponse() Behavior

When a `SipResponse` arrives, `HandleResponse()` applies state machine validation before updating:

```csharp
public void HandleResponse(SipResponse response)
{
    // Determine target state from response status code
    var targetState = response.StatusCode switch
    {
        >= 100 and < 200 => CallLegState.ProvisionalResponse,
        >= 200 and < 300 => CallLegState.Confirmed,
        >= 300 => CallLegState.Failed,
        _ => _state  // Unknown code, no state change
    };

    // Only transition if valid
    if (IsValidTransition(_state, targetState))
    {
        _state = targetState;
    }
    else if (targetState != _state)
    {
        // Late response rejected: log warning, no state change
        _logger.LogWarning("Attempted invalid transition {Current} → {Target}",
            _state, targetState);
    }
}
```

**Examples**:
- Late 1xx (183) after 2xx (200) confirmed dialog: `ProvisionalResponse` state not changed, warning logged
- Multiple 1xx responses: `ProvisionalResponse → ProvisionalResponse`, no-op allowed
- Error response after confirmation: `Confirmed` state unchanged, warning logged

### TransitionToState() Behavior

For explicit state transitions (e.g., sending BYE), validation is stricter and throws on error:

```csharp
internal void TransitionToState(CallLegState newState)
{
    if (!IsValidTransition(_state, newState))
    {
        throw new InvalidOperationException(
            $"Invalid state transition from {_state} to {newState}");
    }
    _state = newState;
}
```

**Examples**:
- Backward transition from `Confirmed` to `Initial`: throws `InvalidOperationException`
- Valid transition from `Confirmed` to `Terminating` (sending BYE): succeeds

## B2BUA Symmetry Requirement

Per RFC3261 Section 12, dialog state is a **mutual property** of both UAC and UAS legs. Both legs must remain in the same state throughout the dialog lifecycle.

### Routing Response Symmetric Updates

In `CallLegOrchestrator`, all routing methods update **both** legs symmetrically:

```csharp
public SipResponse? RouteProvisionalResponse(string callId, SipResponse response)
{
    var (uacLeg, uasLeg) = GetLegs(callId);

    // Both legs updated identically
    uacLeg.HandleResponse(response);  // 1xx → ProvisionalResponse
    uasLeg.HandleResponse(response);  // 1xx → ProvisionalResponse

    return response;
}

public SipResponse? RouteFinalResponse(string callId, SipResponse response)
{
    var (uacLeg, uasLeg) = GetLegs(callId);

    // Both legs confirmed with same method
    uacLeg.HandleResponse(response);  // 2xx → Confirmed
    uasLeg.HandleResponse(response);  // 2xx → Confirmed

    return response;
}
```

**Why Symmetry Matters**:
1. Dialog identity is `(Call-ID, LocalTag, RemoteTag)` — established on both sides
2. State misalignment causes message routing errors
3. Either side receiving late responses must handle them identically

## Testing the State Machine

### Unit Test Categories

1. **Valid Transitions**: Verify each valid transition executes without exception
2. **Invalid Transitions**: Verify backward/invalid transitions throw or log warnings appropriately
3. **Late Response Handling**: Verify late responses don't downgrade state
4. **Symmetry Verification**: Verify both legs in pair have identical state after routing
5. **Edge Cases**: Multiple 1xx, rapid status code changes, unknown codes

### Example Test Patterns

```csharp
[Fact]
public void HandleResponse_WithLate1xxAfter2xx_StateRemains Confirmed()
{
    var leg = CreateCallLeg();

    // Confirm dialog
    leg.HandleResponse(new SipResponse(200, "OK", ...));
    leg.State.ShouldBe(CallLegState.Confirmed);

    // Late provisional should NOT downgrade state
    leg.HandleResponse(new SipResponse(183, "Session Progress", ...));
    leg.State.ShouldBe(CallLegState.Confirmed); // Still confirmed
}

[Fact]
public void TransitionToState_Backward_ThrowsInvalidOperationException()
{
    var leg = CreateCallLeg();
    leg.HandleResponse(new SipResponse(200, "OK", ...)); // Confirmed

    // Backward transition should throw
    Should.Throw<InvalidOperationException>(() =>
        leg.TransitionToState(CallLegState.Initial));
}
```

## Relationship to Dialog Class

The `CallLeg` class and `Dialog` class (from Block 1) serve different purposes:

| Aspect | CallLeg | Dialog |
|--------|---------|--------|
| **Role** | One side of B2BUA | Dialog manager (generic) |
| **Scope** | Single leg (UAC or UAS) | Can represent either side |
| **State Enum** | `CallLegState` (7 states) | `DialogState` (similar) |
| **Symmetry** | Always paired with counterpart | Standalone |
| **Responsibility** | State tracking + routing | Overall dialog lifecycle |

**Design Note**: In a future refactor, `CallLeg` could become a thin wrapper over `Dialog` to eliminate duplication. Currently, both maintain parallel state machines for architectural clarity.

## References

- RFC3261 Section 12: Dialog Usage (https://tools.ietf.org/html/rfc3261#section-12)
- RFC3261 Section 12.1: Dialog State: https://tools.ietf.org/html/rfc3261#section-12.1
- RFC3261 Section 12.2.1.1: Receiving the Final Response: https://tools.ietf.org/html/rfc3261#section-12.2.1.1

---

**Document Version**: 1.0
**Last Updated**: 2026-02-25
**Author**: Claude (TDD Implementation)
**Status**: Complete (Covers r1-r4 implementations)
