# RFC 3261 - Initiating a Session (INVITE)

## 13. Initiating a Session

The INVITE request initiates a session. It's the most complex method in SIP due to the 3-way handshake for reliability.

### INVITE Transaction Overview

```
UAC                                              UAS
 |                                                 |
 |------------------- INVITE (offer) -------------->|
 |<-------------- 100 Trying (optional) -----------|
 |<-------------- 180 Ringing (optional) ----------|
 |<-------------- 183 Session Progress (optional)-|
 |                                                 |
 |<---------------- 200 OK (answer) ---------------|
 |------------------- ACK (confirm) -------------->|
 |                                                 |
 |================= Media Session ================|
 |                                                 |
 |<----------------- BYE ---------------------------|
 |------------------- 200 OK --------------------->|
 |                                                 |
```

---

## 13.2 UAC Processing

### 13.2.1 Creating the Initial INVITE

#### Required Headers

| Header | Value |
|--------|-------|
| **Request-URI** | AOR of callee or callee's contact |
| **To** | Callee's AOR |
| **From** | Caller's AOR |
| **Call-ID** | Unique identifier |
| **CSeq** | Sequence number with method INVITE |
| **Max-Forwards** | 70 |
| **Via** | Transport, address, port, branch |
| **Contact** | Caller's contact for subsequent requests |

#### Contact Header

- **Must** include Contact header in INVITE
- Contains SIP or SIPS URI with **global scope** (not bound to specific call)
- Used for subsequent requests (re-INVITE, ACK, BYE)

#### SDP Offer

The INVITE typically contains an SDP (Session Description Protocol) body describing:
- Media types (audio, video)
- Codecs
- IP address and port for receiving media
- Media attributes

```
INVITE sip:bob@biloxi.com SIP/2.0
Via: SIP/2.0/UDP pc33.atlanta.com;branch=z9hG4bK776asdhds
Max-Forwards: 70
To: Bob <sip:bob@biloxi.com>
From: Alice <sip:alice@atlanta.com>;tag=1928301774
Call-ID: a84b4c76e66710@pc33.atlanta.com
CSeq: 314159 INVITE
Contact: <sip:alice@pc33.atlanta.com>
Content-Type: application/sdp
Content-Length: 142

v=0
o=alice 2890844526 2890844526 IN IP4 pc33.atlanta.com
s=Session
c=IN IP4 pc33.atlanta.com
t=0 0
m=audio 49170 RTP/AVP 0
a=rtpmap:0 PCMU/8000
```

---

### 13.2.2 Processing INVITE Responses

#### 1xx Responses (Provisional)

| Response | Meaning |
|----------|---------|
| **100 Trying** | Request received, locating destination |
| **180 Ringing** | UA is alerting user |
| **181 Call Is Being Forwarded** | Call is being forwarded |
| **182 Queued** | Call queued, waiting |
| **183 Session Progress** | Progress info, early media possible |

- **No ACK sent** for 1xx responses (except if 100rel required)
- UAC can play early media if received with SDP

#### 3xx Responses (Redirection)

| Response | Action |
|----------|--------|
| **300 Multiple Choices** | Present alternatives to user |
| **301 Moved Permanently** | Retry with new Contact |
| **302 Moved Temporarily** | Retry with new Contact (short TTL) |
| **305 Use Proxy** | Must use specified proxy |
| **380 Alternative Service** | Call failed, alternatives |

- Contact header contains alternative addresses
- May retry automatically or prompt user

#### 4xx, 5xx, 6xx Responses (Failure)

| Response | Meaning |
|----------|---------|
| **4xx** | Client error - don't retry same request |
| **5xx** | Server error - may retry to different server |
| **6xx** | Global failure - won't succeed anywhere |

#### 2xx Responses (Success)

- **Dialog created** upon receiving 2xx
- **Must send ACK** within 60 seconds
- ACK is sent **directly** to Contact in 200 OK (not via proxies)

**Critical**: 2xx responses to INVITE are handled specially:
- UAS **retransmits** 200 OK until ACK received
- UAC **must ACK** each 2xx received
- Different from other responses

---

## 13.3 UAS Processing

### 13.3.1 Processing the INVITE

1. **Authenticate** caller (if required)
2. **Check authorization**
3. **Determine user availability** (ring, busy, etc.)
4. **Generate offer/answer** if no SDP in request

### 13.3.2 Progress

- Send 100 Trying quickly to stop retransmissions from UAC
- Send 180 Ringing when user is alerted
- Send 183 Session Progress for progress information
- These can include SDP for early media

### 13.3.3 The INVITE is Redirected

- If forwarding: return 3xx with Contact
- Original caller can try new address

### 13.3.4 The INVITE is Rejected

- Return appropriate 4xx/5xx/6xx response
- Early dialog terminated

### 13.3.5 The INVITE is Accepted

1. Create dialog (add To tag)
2. Include Contact in response
3. Generate SDP answer (if request had offer)
4. Send 200 OK
5. Wait for ACK

---

## ACK Processing

### For 2xx Responses

- UAC **must** send ACK
- ACK destination is Contact URI from 200 OK
- Can be sent directly (peer-to-peer) bypassing proxies

```
ACK sip:bob@192.0.2.4 SIP/2.0
Via: SIP/2.0/UDP pc33.atlanta.com;branch=z9hG4bK776asdhds
From: Alice <sip:alice@atlanta.com>;tag=1928301774
To: Bob <sip:bob@biloxi.com>;tag=a6c85cf
Call-ID: a84b4c76e66710@pc33.atlanta.com
CSeq: 314159 ACK
```

### For Non-2xx Responses

- ACK is sent as part of transaction layer
- ACK goes back through same path as INVITE

---

## Transaction Lifecycle for INVITE

```
UAC (Client Transaction)                    UAS (Server Transaction)
     |                                              |
     |--- INVITE (Calling) ----------------------->| (Proceeding)
     |<-- 1xx --------------------------------------| (Proceeding)
     |<-- 2xx --------------------------------------| (Terminated)
     |--- ACK (Acknowledging) --------------------->| (Completed)
     |                                              |
     |================= Media =====================|
     |                                              |
     |--- BYE ------------------------------------->|
     |<-- 200 OK -----------------------------------|
```

---

## Related Sections

- [Messages](03-messages.md) - Request/Response formats
- [Dialogs](06-dialogs.md) - Dialog creation
- [Modifying Session](08-modifying-session.md) - re-INVITE
- [Terminating Session](09-terminating-session.md) - BYE
- [Transactions](11-transactions.md) - INVITE transaction state machine
- [Response Codes](14-response-codes.md) - Response code meanings
