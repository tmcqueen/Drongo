# Drongo Concurrency Model

## Core Principle

Concurrency is isolated by scope:
- Transport scope
- Transaction scope
- Dialog scope
- Application scope

Each layer owns its concurrency rules. No layer leaks threading semantics upward.

---

## 1. Global Concurrency Philosophy

Drongo is:
- Fully async/await
- Thread-agnostic (no thread affinity)
- Lock-minimal
- Actor-inspired at dialog level
- Backpressure-aware

### Hard Guarantees

- A given SIP transaction is processed serially
- A given dialog is processed serially
- Separate dialogs may execute in parallel
- No user handler is executed concurrently for the same dialog

---

## 2. Transport Layer Concurrency

### UDP

**Model:**
- N socket receive loops (configurable)
- Each loop parses packet → enqueues to dispatch pipeline
- No per-socket locking

**Implementation strategy:**
- SocketAsyncEventArgs pooling
- Channel<T> or lock-free queue to dispatch layer

**Key rule:** Transport threads do not execute application middleware. They only parse and enqueue.

### TCP (Phase 2)

**Model:**
- One connection = one receive loop
- Each connection maintains framing state
- Parsed messages dispatched independently

**Concurrency rules:**
- Multiple connections process in parallel
- Messages from a single TCP connection are processed in order
- Dispatch layer does not assume connection affinity

### Backpressure

- Dispatch queue bounded
- If overwhelmed:
  - Drop UDP (RFC-compliant behavior)
  - Apply TCP flow control
  - Emit overload metrics

---

## 3. Parser Layer

- **Stateless.** Zero shared mutable state
- **Buffer pooling.** No locks
- **Concurrency model:** Fully parallel across messages

---

## 4. Transaction Layer

This is the first serialized boundary.

**Rule:** A transaction is single-threaded.

**Implementation model:**
- `ConcurrentDictionary<TransactionKey, TransactionActor>`
- Each transaction has a mailbox (`Channel<SipMessage>`)
- One consumer loop per transaction

```csharp
class TransactionActor
{
    private readonly Channel<SipMessage> _mailbox;

    public async Task RunAsync()
    {
        await foreach (var message in _mailbox.Reader.ReadAllAsync())
        {
            Handle(message);
        }
    }
}
```

**Guarantee:** Timer events and network messages are processed sequentially. No internal locking needed inside transaction.

---

## 5. Dialog Layer (Actor Model)

Dialogs behave like lightweight actors.

**Rule:** A dialog processes events serially.

**Sources of events:**
- Incoming requests
- Transaction completions
- Timer events
- Application commands (Transfer, Hangup, etc.)

Each dialog has:
- `Channel<DialogEvent>`
- Single execution loop

### Critical Guarantee

User code inside `app.MapInvite(...)` will never execute concurrently for the same dialog.

Even if:
- Re-INVITE arrives
- UPDATE arrives
- Timer fires

They queue.

---

## 6. Middleware Pipeline Concurrency

Middleware is:
- Executed per request
- Isolated per dialog

**Parallelism occurs:**
- Across dialogs
- Across transactions
- Across connections

**Never** within the same dialog execution chain.

---

## 7. Registrar Concurrency

Default in-memory registrar:
- `ConcurrentDictionary<string, ContactSet>`
- ContactSet guarded by fine-grained lock OR immutable swap

**Safer approach:** Use immutable collections:
- `ImmutableDictionary<string, ContactBinding>`
- Replace atomically

**Guarantee:**
- REGISTER processing serialized per AoR
- Concurrent different AoRs allowed

---

## 8. Media Concurrency

Phase 1 (NAudio + RTP):

Each call leg has:
- Dedicated RTP receive loop
- MediaSession state machine
- DTMF queue

**Rules:**
- MediaSession is single-threaded logically
- RTP packets may arrive concurrently but are funneled through a serialized processor
- No cross-dialog media sharing in Phase 1

---

## 9. Scripting Concurrency

Per-call execution context.

**Execution guarantees:**
- One active script continuation at a time per dialog
- Await does not introduce parallelism
- CancellationToken tied to dialog termination

If call ends:
- Script is canceled
- Media session disposed
- Dialog actor terminates

---

## 10. Application-Level Guarantees

What developers can rely on:

| Guarantee | Description |
|-----------|-------------|
| 1 | Handlers for a dialog are not reentrant |
| 2 | No thread affinity — don't assume thread-static storage |
| 3 | Await is safe — state remains consistent |
| 4 | Transfer/Hangup are idempotent |

---

## 11. Threading Model Summary

| Scope | Parallel? | Serialized? | Lock Required? |
|-------|-----------|-------------|----------------|
| Transport | Yes | No | No |
| Parser | Yes | No | No |
| Transaction | Across transactions | Within transaction | No |
| Dialog | Across dialogs | Within dialog | No |
| Registrar (per AoR) | Across AoRs | Within AoR | Minimal |
| Media (per call) | Across calls | Within call | No |

---

## 12. Failure Containment

If:
- A transaction throws
- A dialog handler throws

Then:
- Exception caught inside actor loop
- Dialog terminated gracefully
- No crash propagation

Drongo must never allow:
- Unobserved task exceptions
- Background thread crashes
- Timer thread exceptions escaping

---

## 13. Horizontal Scaling Model

Because dialogs are isolated actors:
- Stateless core
- Registrar pluggable
- Media local to instance
- Admin API instance-specific

**Horizontal scaling via:**
- Load balancer
- Shared registrar (Phase 2+)
- Sticky routing (optional)

---

## 14. Why This Model Works

- Avoids fine-grained locking
- Eliminates reentrancy bugs
- Makes state machines tractable
- Mirrors telecom switch design patterns
- Aligns with .NET async best practices

---

## 15. The Most Important Design Rule

> **Dialog is the highest concurrency boundary.**
> 
> Everything inside a dialog must be sequential.
> Everything outside a dialog can be parallel.

If you violate that rule, race conditions appear immediately.
