# Phase 1 Implementation Plan: Core Signaling MVP

## Phase Goal
Deterministic, high-performance SIP B2BUA over UDP with basic IVR capability.

---

## 1. Solution Setup

### 1.1 Projects
| Project | Purpose |
|---------|---------|
| `src/Drongo` | Main application host, ASP.NET Core-style startup |
| `src/Drongo.Core` | Core SIP engine - transactions, dialogs, transport |
| `src/Drongo.Media` | NAudio media sessions (play, record, DTMF) |
| `tests/Drongo.Core.Tests` | Unit tests for state machines, parsing |
| `tests/Drongo.Media.Tests` | Media session tests |

### 1.2 Dependencies
- **.NET 10+** with C# 13+ and nullable reference types
- **Microsoft.Extensions.*** (Hosting, DependencyInjection, Logging, Configuration)
- **NAudio** (media playback/recording)

---

## 2. Foundation Layer

### 2.1 SIP Data Types
- `SipUri` - immutable URI record
- `SipRequest` - method, uri, headers, body
- `SipResponse` - status code, reason, headers, body
- `ContactBinding` - binding record with expiration
- `SipMethod` enum (INVITE, ACK, BYE, CANCEL, REGISTER, OPTIONS, etc.)

### 2.2 SIP Parser (Stateless)
- Parse request line → method + uri
- Parse response line → status + reason
- Parse headers (dictionary)
- Handle SDP body parsing (minimal for Phase 1)
- **Test**: malformed packet handling

### 2.3 Timer Infrastructure
- Abstract timer interface (`ITimerFactory`)
- RFC Appendix A timers: T1=500ms, T2=4s, T4=5s
- Transaction timers: A, B, C, D, E, F, G, H, I, J, K

---

## 3. Transport Layer (UDP)

### 3.1 UdpTransport
- Socket receive loops (configurable count, default: processor count)
- SocketAsyncEventArgs pooling
- Dispatch queue: `Channel<SipMessage>` to parser

### 3.2 Message Dispatch
- Parse → validate minimal required headers
- Match to existing transaction OR create new server transaction

---

## 4. Transaction Layer (RFC 17)

### 4.1 Client Transaction State Machine

**INVITE Client Transaction - States:**
| State | Description |
|-------|-------------|
| Calling | Initial state - INVITE request sent |
| Proceeding | Provisional response (1xx) received |
| Completed | Final response (300-699) received |
| Terminated | Transaction complete/destroyed |

**Non-INVITE Client Transaction - States:**
| State | Description |
|-------|-------------|
| Trying | Initial state - request sent |
| Proceeding | Provisional response (1xx) received |
| Completed | Final response (200-699) received |
| Terminated | Transaction complete/destroyed |

**Events:**
- TU initiates request
- Timer A/E fires (retransmit)
- Timer B/F fires (timeout)
- Transport error
- 1xx/2xx/3xx-6xx responses

**Implementation:**
- Actor model: `Channel<TransactionMessage>` per transaction
- Deterministic - no internal locking

### 4.2 Server Transaction State Machine

**INVITE Server Transaction - States:**
| State | Description |
|-------|-------------|
| Proceeding | Initial state - request received, awaiting TU response |
| Completed | Final non-2xx response sent, awaiting ACK |
| Confirmed | ACK received, absorbing late ACKs |
| Terminated | Transaction complete/destroyed |

**Non-INVITE Server Transaction - States:**
| State | Description |
|-------|-------------|
| Trying | Initial state - request received |
| Proceeding | Provisional response sent |
| Completed | Final response sent |
| Terminated | Transaction complete/destroyed |

**Events:**
- Request received
- TU sends response
- ACK received (INVITE)
- Timer G/H/I/J fires

---

## 5. Dialog Layer (RFC 12)

### 5.1 Dialog State Machine
**States:**
| State | Description |
|-------|-------------|
| Early | Dialog created via provisional response (101-199) to INVITE |
| Confirmed | Dialog created via 2xx final response to INVITE |

### 5.2 B2BUA Dialog Model
- **Call Leg (UAC side)**: Outgoing INVITE to destination
- **Agent Leg (UAS side)**: Incoming INVITE from caller
- Both legs share single dialog context for state coordination

### 5.3 Dialog Implementation
- Actor: `Channel<DialogMessage>` per dialog
- Handles: INVITE, ACK, BYE, re-INVITE, UPDATE
- Timer: Session timer support (optional Phase 1)

---

## 6. Registrar (RFC 10)

### 6.1 In-Memory Registrar
- `ConcurrentDictionary<string, ContactSet>`
- Bindings with expiration
- Single AoR per user (no multiple contacts for Phase 1)

### 6.2 Register Processing
- Add/refresh bindings
- Remove bindings (explicit or expired)
- Return contacts on lookup

---

## 7. Media Layer (Phase 1)

### 7.1 IMediaSession
- `StartAsync()` - begin media handling
- `PlayAsync(filePath)` - play WAV/PCM
- `RecordAsync(filePath)` - record
- `ReceiveDtmfAsync(timeout)` - collect digits
- `StopAsync()` - terminate

### 7.2 RTP Handling (Minimal)
- Receive RTP packets on negotiated port
- Basic G.711 payload handling
- DTMF RFC 2833 detection

---

## 8. Hosting & Middleware

### 8.1 DrongoApplication
- `CreateBuilder(args)` - fluent setup
- `MapInvite(pattern, handler)` - route IVR scripts
- `MapRegister(handler)` - registration handler

### 8.2 Contexts
- `DrongoContext` - base
- `InviteContext` - call context with Answer/Transfer/Hangup
- `RegisterContext` - registration context

### 8.3 Middleware Pipeline
- Per-request processing
- Logging scope by Call-ID
- Exception handling

---

## 9. API Surface

The implementation should expose:
- `IDrongoBuilder` / `IDrongoServerBuilder`
- `IDialog` interface
- `IRegistrar` interface
- `IMediaSession` / `IMediaSessionFactory`
- Observability: `IDrongoMetrics`

---

## 10. Testing Strategy

### 10.1 Unit Tests
- Transaction state machines: each state + event combination
- Parser: valid/invalid messages
- Dialog creation/teardown

### 10.2 Integration Tests
- UDP round-trip: INVITE → 180 → 200 → ACK → BYE → 200
- Registrar: register → resolve → unregister

---

## 11. Task Breakdown (Execution Order)

1. **Solution & Data Types** - Projects, SIP records, parser skeleton
2. **Timer Infrastructure** - Abstract timers, RFC defaults
3. **UDP Transport** - Receive loop, dispatch to parser
4. **Server Transaction** - State machine implementation
5. **Client Transaction** - State machine implementation  
6. **Dialog Layer** - B2BUA dialog with two legs
7. **Registrar** - In-memory implementation
8. **Media Session** - NAudio play/record/DTMF
9. **Hosting** - DrongoApplication, MapInvite, contexts
10. **Middleware** - Pipeline, logging
11. **Integration Tests** - Full call flow

---

## Open Questions

1. **SDP Handling**: How complex should SDP parsing/generation be for Phase 1? Just basic audio port/codec, or full offer/answer?
2. **Authentication**: Any auth requirements for REGISTER (digest), or anonymous for MVP?
3. **Logging**: Structured logging with Serilog, or built-in Microsoft.Extensions.Logging?
