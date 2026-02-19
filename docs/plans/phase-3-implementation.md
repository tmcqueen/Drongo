# Phase 3 Implementation Plan: Security, WebRTC, and Admin Framework

## Phase Goal
Platform maturity and modern edge integration.

---

## 1. TLS Support

### 1.1 SIP over TLS
- TLS listener configuration
- Certificate configuration via DI
- SNI support

### 1.2 Mutual TLS (Optional)
- Client certificate validation
- Trust chain management

### 1.3 Configuration
```csharp
IDrongoServerBuilder.ListenTls(int port, Action<TlsOptions> configure);
```

---

## 2. WebRTC Integration

### 2.1 Architecture Overview

WebRTC introduces significant complexity and requires new components:

```
SIP (Drongo Core)  ←→  WebRTC Gateway Layer  ←→  Browser/Client
```

### 2.2 SIP over WebSocket (RFC 7118)

- WebSocket transport
- Connection session mapping
- Subprotocol: websocket

### 2.3 SDP Translation Layer

**New Interfaces:**
```csharp
public interface ISdpTranslator
{
    SdpOffer TranslateToWebRtc(SdpOffer sdp);
    SdpOffer TranslateFromWebRtc(SdpOffer sdp);
}
```

- Convert SIP SDP to WebRTC SDP
- Handle ICE candidates
- Codec mapping (opus, vp8/9, etc.)

### 2.4 DTLS-SRTP

- DTLS handshake for key exchange
- SRTP encryption/decryption
- Key management

### 2.5 ICE Handling

- STUN/TURN integration
- Candidate gathering
- ICE lite support (for server)
- Connectivity checks

### 2.6 WebRTC Session

**New Interface:**
```csharp
public interface IWebRtcSession
{
    Task<SessionDescription> CreateOfferAsync();
    Task<SessionDescription> CreateAnswerAsync(SessionDescription offer);
    Task SetRemoteDescriptionAsync(SessionDescription description);
    Task AddIceCandidateAsync(IceCandidate candidate);
    
    event Action<IceCandidate>? OnLocalCandidate;
    event Action<byte[]>? OnRtpPacket;
}
```

---

## 3. Admin Framework

### 3.1 Architecture

Modeled loosely after ASP.NET Core + Identity UI.

**Components:**
- Drongo.Admin.Core - Domain models, services
- Drongo.Admin.Web - ASP.NET Core-based UI
- Admin API - REST endpoints
- Role-based access control
- Live event stream (SignalR or WebSockets)

### 3.2 API Endpoints

```
GET    /admin/dialogs              # List active dialogs
GET    /admin/dialogs/{id}        # Get dialog details
POST   /admin/dialogs/{id}/terminate  # Terminate dialog
GET    /admin/registrations        # List registrations
GET    /admin/registrations/{aor} # Get AoR details
POST   /admin/registrations/{aor}/expire  # Force expire
GET    /admin/transports           # List transport connections
GET    /admin/metrics              # System metrics
GET    /admin/config               # View configuration
```

### 3.3 Observability

- Prometheus metrics endpoint
- Structured logs with correlation
- Per-call tracing (Call-ID)
- Event streaming for live updates

### 3.4 Security

- Admin authentication
- Role-based access (viewer, operator, admin)
- Audit logging

---

## 4. Task Breakdown

1. **TLS Transport** - SIP over TLS, SNI, certificate config
2. **WebSocket Transport** - SIP over WebSocket (RFC 7118)
3. **SDP Translator** - Convert between SIP and WebRTC SDP
4. **DTLS Handshake** - Key exchange for SRTP
5. **ICE Handling** - STUN/TURN, candidate negotiation
6. **WebRTC Session** - Full browser interop
7. **Admin API** - REST endpoints for management
8. **Admin Web UI** - Dashboard for operations
9. **Live Events** - SignalR/WebSocket event stream
10. **Metrics** - Prometheus integration

---

## Open Questions

1. **WebRTC**: Which WebRTC library? (e.g., WebRTC.NET, custom implementation, or third-party media server?)
2. **Admin UI**: Blazor WebAssembly or MVC?
3. **TURN Server**: External TURN service or integrated?
