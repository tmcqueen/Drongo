# RFC 3261 - Protocol Overview

## 4. Overview of Operation

### Transaction Model

SIP uses an HTTP-like **request/response transaction model**. Each transaction consists of:
1. A **request** that invokes a particular method (function) on the server
2. At least one **response**

### Basic Call Flow

```
Alice's          atlanta.com         biloxi.com         Bob's
softphone          proxy               proxy           SIP Phone
   |                  |                   |                 |
   |   INVITE F1     |                   |                 |
   |---------------->|   INVITE F2       |                 |
   |  100 Trying F3  |------------------>|   INVITE F4     |
   |<----------------|  100 Trying F5    |---------------->|
   |                 |<------------------|  180 Ringing F6 |
   |  180 Ringing F7 |<------------------|                 |
   |<----------------|  180 Ringing F8   |<----------------|
   |  180 Ringing F9 |<------------------|  200 OK F10    |
   |<----------------|    200 OK F11     |<----------------|
   |    200 OK F12   |<------------------|                |
   |<----------------|                   |                 |
   |                       ACK F13                      |
   |-------------------------------------------------->|
   |                   Media Session                     |
   |<=================================================>|
   |                       BYE F14                      |
   |-------------------------------------------------->|
   |                     200 OK F15                     |
   |<--------------------------------------------------|
   |                                                  |
```

### Key SIP Methods

| Method | Purpose |
|--------|---------|
| **INVITE** | Initiate a session (call) |
| **ACK** | Confirm final response to INVITE |
| **BYE** | Terminate a session |
| **CANCEL** | Cancel a pending request |
| **OPTIONS** | Query capabilities of a UAS |
| **REGISTER** | Register a location with a registrar |

### The SIP Trapezoid

The typical arrangement with proxies is called the "SIP trapezoid":
1. UAC sends INVITE to outbound proxy
2. Outbound proxy routes to inbound proxy
3. Inbound proxy delivers to UAS
4. Responses and subsequent requests can flow directly

### Registrations

- Users **REGISTER** their current location with a **Registrar**
- Registrar binds the user's AOR (Address-of-Record) to a **Contact** address
- **Proxy servers** use this binding to locate the user

### Record-Route

Proxies can insert **Record-Route** header to remain in the signaling path for the duration of the dialog. This allows proxies to:
- Track session state
- Enable topology hiding
- Provide services throughout the call

---

## 5. Structure of the Protocol

### Layered Architecture

SIP is organized in layers:

```
┌─────────────────────────────────────┐
│     Transaction User (TU)           │  ← Creates transactions, decides routing
├─────────────────────────────────────┤
│     Transaction Layer               │  ← Retransmissions, matching, timeouts
├─────────────────────────────────────┤
│     Transport Layer                 │  ← How clients/servers send/receive
├─────────────────────────────────────┤
│     Syntax and Encoding Layer       │  ← BNF grammar (Section 25)
└─────────────────────────────────────┘
```

### Layer Responsibilities

1. **Syntax/Encoding Layer**: Parsing SIP messages according to BNF grammar
2. **Transport Layer**: Sending and receiving messages over UDP, TCP, TLS, or SCTP
3. **Transaction Layer**: 
   - Handles retransmissions of requests
   - Matches responses to requests
   - Handles timeouts
   - Provides reliability for unreliable transports
4. **Transaction User (TU)**: The application layer that uses transactions

---

## 6. Definitions

### Core Terms

| Term | Definition |
|------|------------|
| **Address-of-Record (AOR)** | A SIP or SIPS URI that points to a domain with a location service. Example: `sip:bob@biloxi.com` |
| **Back-to-Back User Agent (B2BUA)** | A UA that receives a request as a UAS, generates a new request as a UAC, and maintains dialog state for both dialogs |
| **Call-ID** | A unique identifier for a call, used to correlate messages within a dialog |
| **Dialog** | A peer-to-peer SIP relationship between two UAs, identified by Call-ID + local tag + remote tag |
| **Location Service** | A database that maps AORs to contact addresses |
| **Message Body** | The payload of a SIP message, typically SDP for session descriptions |
| **Proxy Server** | An intermediate entity that forwards requests on behalf of the requestor |
| **Registrar** | A server that processes REGISTER requests and binds AORs to location addresses |
| **Transaction** | A request and its corresponding responses |
| **User Agent Client (UAC)** | A client application that initiates SIP requests |
| **User Agent Server (UAS)** | A server application that receives requests and sends responses |

### Tags

- **From tag**: Added by the UAC to identify its side of the dialog
- **To tag**: Added by the UAS to identify its side of the dialog
- The combination of Call-ID + From tag + To tag uniquely identifies a dialog

---

## Related Sections

- [Introduction](01-introduction.md) - Intro and terminology
- [Messages](03-messages.md) - Request/Response formats
- [Dialogs](06-dialogs.md) - Dialog creation and management
- [Transactions](11-transactions.md) - Transaction state machines
