# Phase 2 Implementation Plan: Transport & Signaling Maturity

## Phase Goal
Improve interoperability and real-world deployment readiness. Make Drongo production-capable in enterprise SIP environments.

---

## 1. TCP Transport

### 1.1 Connection Management
- Accept TCP connections
- Per-connection state (receive buffer, framing state)
- Connection lifecycle: connect → receive → close

### 1.2 Framing (RFC 3261 Section 18)
- Detect message boundaries using Content-Length header
- Handle pipelined requests
- Handle partial messages

### 1.3 Keep-Alive
- CRLF keep-alive per RFC 3261 Section 18.3
- Configurable interval

---

## 2. SIP Enhancements

### 2.1 PRACK (RFC 3262)
- Reliable provisional responses
- Require: 100rel option tag
- RAck/RSeq headers
- Transaction layer must track reliability

### 2.2 UPDATE (RFC 3311)
- Mid-dialog offer/answer
- No effect on dialog state
- Uses same transaction machinery

### 2.3 REFER (RFC 3515)
- Call transfer
- Refer-To header
- Subscription handling (NOTIFY)
- Transfer target: receives INVITE with Replaces

### 2.4 Parallel Forking
- Fork to multiple targets simultaneously
- Collect responses
- Accept first 2xx, cancel others
- Handle no answer / all failures

### 2.5 Transaction Timer Compliance
- RFC Appendix A timers with full compliance
- Timer C for proxies (configurable)
- Proper retransmission behavior

### 2.6 GRUU (RFC 5627)
- Globally Routable User Agent URIs
- Instance ID support
- Public/GRUU vs instance/temp GRUU

---

## 3. Registrar Enhancements

### 3.1 Redis-Backed Registrar
- Distributed registration storage
- Atomic operations
- Expiration handling
- Failover support

### 3.2 Registrar Extensibility
- IRegistrar interface already defined
- Plugable backends: InMemory, Redis, SQL
- Configuration-driven selection

---

## 4. Media Enhancements

### 4.1 Early Media Handling
- 18x responses with SDP
- Media before final response
- Ringback tone injection

### 4.2 Media Bridging
- Two-leg mixing
- Conference calls
- Direct media path between endpoints

### 4.3 RTP Session Lifecycle
- Proper setup/teardown
- RTCP support (optional)
- NAT handling considerations

---

## 5. Performance Goals

### 5.1 Scale Targets
- 100k+ registrations
- 20k+ concurrent dialogs
- Connection pooling for TCP

### 5.2 Backpressure
- Queue depth monitoring
- Overload protection
- Resource cleanup under pressure

---

## 6. Dialog Event Hooks

### 6.1 Event System
- Dialog created/destroyed
- Dialog state changed
- Call started/ended
- Registration changed

### 6.2 Observability
- Hooks for metrics collection
- Integration points for logging

---

## 7. Task Breakdown

1. **TCP Transport** - Connection management, framing, keep-alive
2. **PRACK Support** - Reliable 1xx, RAck/RSeq
3. **UPDATE Support** - Mid-dialog offer/answer
4. **REFER Support** - Call transfer, NOTIFY handling
5. **Parallel Forking** - Fork logic, response aggregation
6. **Timer Compliance** - Full RFC timer implementation
7. **GRUU Support** - Instance IDs, temp/public GRUU
8. **Redis Registrar** - Distributed registration storage
9. **Early Media** - 18x with SDP handling
10. **Media Bridging** - Two-leg mixing
11. **Performance Tuning** - Connection pooling, backpressure

---

## Open Questions

1. **Forking UI**: How should fork targets be configured? Code-based or config?
2. **Redis**: Preferred Redis client? StackExchange.Redis, NRedisStack?
3. **RTCP**: Include RTCP metrics, or just basic quality?
