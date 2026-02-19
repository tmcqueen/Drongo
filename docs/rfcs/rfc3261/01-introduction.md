# RFC 3261 - SIP: Session Initiation Protocol

## 1. Introduction

SIP (Session Initiation Protocol) is an application-layer control protocol for creating, modifying, and terminating sessions with one or more participants. These sessions include Internet telephone calls, multimedia distribution, and multimedia conferences.

### Key Concepts

- **Application-layer control protocol**: SIP operates at the application layer and manages sessions between endpoints
- **Session definition**: An exchange of data between an association of participants
- **Five facets of SIP**:
  1. **User location**: Determination of the end system to be used for communication
  2. **User availability**: Determination of the willingness of the called party to engage in communications
  3. **User capabilities**: Determination of the media and media parameters to be used
  4. **Session setup**: "Ringing", establishment of session parameters at both called and calling party
  5. **Session management**: Including transfer and termination of sessions, modifying session parameters, and invoking services

### Important Rules

- SIP provides **primitives** that can be used to implement different services - it is NOT a vertically integrated communications system
- SIP works with other IETF protocols: RTP (real-time data), RTSP (streaming media), SDP (session descriptions)
- Supports both **IPv4 and IPv6**
- Security services include: denial-of-service prevention, authentication, integrity protection, encryption, and privacy

---

## 2. Overview of SIP Functionality

SIP is an agile, general-purpose tool for creating, modifying, and terminating multimedia sessions. It works independently of underlying transport protocols and without dependency on the type of session being established.

### SIP Works With

| Protocol | Purpose |
|----------|---------|
| RTP | Real-time transport of media data |
| RTSP | Controlling delivery of streaming media |
| SDP | Describing multimedia sessions |
| MEGACO | Controlling PSTN gateways |

### Capabilities

- **Personal mobility**: Users maintain single externally visible identifier regardless of network location
- **Name mapping and redirection services**
- **Session modification**: Media can be added to or removed from existing sessions
- **Multicast conferences**: Can invite participants to existing sessions

---

## 3. Terminology

### RFC 2119 Requirement Levels

| Keyword | Meaning | Implementation |
|---------|---------|----------------|
| **MUST** | Required | Must be implemented |
| **MUST NOT** | Prohibition | Cannot be implemented |
| **SHOULD** | Recommended | Should be implemented unless specific reason not to |
| **SHOULD NOT** | Not Recommended | Should not be implemented unless specific reason |
| **MAY** | Optional | Implementation is choice |

### Key Terms

- **User Agent Client (UAC)**: A client application that initiates SIP requests
- **User Agent Server (UAS)**: A server application that receives SIP requests and sends responses
- **Back-to-Back User Agent (B2BUA)**: A UA that receives requests as a UAS, generates requests as a UAC, and maintains dialog state
- **Proxy Server**: An intermediate entity that acts as both a server and client for requesting services
- **Registrar**: A server that processes REGISTER requests and maintains a location service database
- **Address-of-Record (AOR)**: A SIP or SIPS URI that points to a domain with a location service

---

## Related Sections

- [Messages](03-messages.md) - Request/Response formats
- [Dialogs](../06-dialogs.md) - Dialog creation and management
- [Transactions](../11-transactions.md) - Transaction state machines
