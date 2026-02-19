# Drongo — Phased Delivery Roadmap

## Phase 1 — Core Signaling MVP

**Goal:** Deterministic, high-performance SIP runtime over UDP with basic IVR.

### Transport
- ✅ UDP only
- ❌ No TCP
- ❌ No TLS

### SIP Features
- INVITE, ACK, BYE, CANCEL, REGISTER
- Basic routing
- In-memory registrar
- Single-branch (no parallel fork aggregation)

### Media
- G.711 (PCM)
- Basic RTP handling
- NAudio playback
- IVR: answer + play + DTMF

### Hosting
- IHostBuilder integration
- Middleware pipeline
- Endpoint mapping
- Basic logging + metrics

### Deliverable Outcome

A developer can:
- Accept calls
- Register endpoints
- Play IVR prompts
- Transfer calls
- Run as a container

This establishes signaling correctness before adding transport complexity.

---

## Phase 2 — Transport & Signaling Maturity

**Goal:** Improve interoperability and real-world deployment readiness.

### Transport
- ✅ TCP support
- Keep TLS deferred

**Rationale:** TCP introduces connection state management, keep-alives, and framing issues. It significantly increases parser edge-case handling. It should not contaminate Phase 1's deterministic UDP engine.

### SIP Enhancements
- PRACK, UPDATE, REFER
- Parallel forking
- Fork aggregation logic
- Improved transaction timer compliance
- GRUU
- Registrar extensibility (Redis-backed option)
- Dialog event hooks

### Media
- Early media handling
- Basic media bridging (two-leg mixing)
- Improved RTP session lifecycle management

### Performance Goals

Stable under:
- 100k+ registrations
- 20k+ concurrent dialogs
- Connection pooling for TCP
- Backpressure-aware pipeline

### Deliverable Outcome

Drongo can:
- Operate behind load balancers
- Handle real carrier interop
- Support advanced call flows
- Scale horizontally with distributed registrar

This phase makes Drongo production-capable in enterprise SIP environments.

---

## Phase 3 — Security, WebRTC, and Administrative Framework

**Goal:** Platform maturity and modern edge integration.

### TLS Support

- SIP over TLS
- Certificate configuration via DI
- SNI support
- Mutual TLS (optional)

This introduces:
- Secure trunking
- Enterprise compliance
- Public-facing deployments

TLS is isolated to Phase 3 to avoid early cryptographic lifecycle complexity.

### WebRTC Integration

This is a major architectural expansion.

**Scope:**
- SIP ↔ WebRTC interop
- DTLS-SRTP
- ICE handling
- STUN/TURN integration
- SDP normalization layer
- WebSocket transport (SIP over WebSocket)

**Required Components:**

*WebSocket Transport*
- SIP over WS (RFC 7118)
- Connection session mapping

*Media*
- SRTP
- DTLS handshake
- ICE candidate negotiation
- SDP translation between WebRTC and SIP endpoints

**Architectural Impact**

Introduce:
- `IMediaNegotiator`
- `ISdpTranslator`
- `IWebRtcSession`

WebRTC must live behind abstractions so core SIP engine remains transport-neutral.

### Admin Framework (Drongo.Admin)

This is not just a dashboard. It is an operational control plane.

**Goals:**
- Inspect active dialogs
- View registrations
- Terminate sessions
- View transport connections
- Real-time metrics
- Configuration visibility
- Script deployment management

**Architecture**

Modeled loosely after ASP.NET Core + Identity UI.

**Components:**
- Drongo.Admin.Core
- Drongo.Admin.Web (ASP.NET Core-based UI)
- Admin API (REST)
- Role-based access control
- Live event stream (SignalR or WebSockets)

**API Examples:**
```
GET    /admin/dialogs
GET    /admin/registrations
POST   /admin/dialogs/{id}/terminate
GET    /admin/transports
```

**Observability:**
- Prometheus metrics endpoint
- Structured logs
- Per-call correlation ID
- Event streaming

---

## Feature Summary

| Feature              | Phase 1 | Phase 2 | Phase 3 |
|---------------------|:-------:|:-------:|:-------:|
| UDP                 |    ✅    |    ✅    |    ✅    |
| TCP                 |    ❌    |    ✅    |    ✅    |
| TLS                 |    ❌    |    ❌    |    ✅    |
| Basic Registrar     |    ✅    |    ✅    |    ✅    |
| Distributed Registrar |   ❌    |    ✅    |    ✅    |
| PRACK / REFER       |    ❌    |    ✅    |    ✅    |
| Media Bridging      |    ❌    |    ✅    |    ✅    |
| WebRTC              |    ❌    |    ❌    |    ✅    |
| Admin Framework     |    ❌    |    ❌    |    ✅    |

---

## Architectural Rationale

This staging:
- Protects the transaction engine from early complexity
- Prevents transport and cryptography from destabilizing Phase 1
- Keeps WebRTC isolated to a late-stage expansion
- Introduces operational tooling only after signaling is hardened

---

## Strategic Observation

By Phase 3, Drongo becomes:
- A signaling runtime
- A programmable SIP framework
- A WebRTC gateway
- A manageable telecom platform
- A modern .NET-native softswitch core

That is an ecosystem, not a library.
