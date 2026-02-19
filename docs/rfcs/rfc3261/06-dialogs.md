# RFC 3261 - Dialogs

## 12. Dialogs

A **Dialog** is a peer-to-peer SIP relationship between two UAs. It is identified by the combination of:
- **Call-ID**
- **Local tag** (from the local UA)
- **Remote tag** (from the remote UA)

### Dialog State

| State | Description |
|-------|-------------|
| **Early** | Dialog created by a provisional response (1xx) to INVITE |
| **Confirmed** | Dialog created by a 2xx final response to INVITE |

---

## 12.1 Creation of a Dialog

### Dialog Identification

Each dialog maintains:

| Field | Description |
|-------|-------------|
| **Call-ID** | From the request/response |
| **Local Tag** | From local UA (From tag for UAC, To tag for UAS) |
| **Remote Tag** | From remote UA (To tag for UAC, From tag for UAS) |
| **Local Sequence Number** | CSeq from local UA |
| **Remote Sequence Number** | CSeq from remote UA |
| **Local URI** | From header URI |
| **Remote URI** | To header URI |
| **Remote Target** | From Contact header in request/response |
| **Secure Flag** | Whether dialog is over TLS (sips:) |
| **Route Set** | Ordered list of URIs from Record-Route headers |

---

## 12.1.1 UAS Behavior (Creating Dialog)

When a UAS receives an INVITE and creates a dialog:

### MUST do:
1. Copy **Record-Route** header from request to response
2. Add **Contact** header to response
3. Set **route set** from Record-Route (preserving order)
4. Set **remote target** from Contact header in request
5. Set **remote sequence number** from CSeq in request

### Dialog ID:
- **Call-ID**: From request
- **Local tag**: Tag added to To header in response
- **Remote tag**: From tag in From header of request

---

## 12.1.2 UAC Behavior (Creating Dialog)

When a UAC receives a successful response and creates a dialog:

### MUST do:
1. Include **Contact** header in original INVITE
2. Construct **route set** from Record-Route in response (reversed order)
3. Set **remote target** from Contact header in response

### Dialog ID:
- **Call-ID**: From request
- **Local tag**: Tag added to From header in request
- **Remote tag**: From To header in response

---

## 12.2 Requests within a Dialog

### Key Rules

1. **CSeq MUST be strictly monotonically increasing** - Each new request increments the CSeq number
2. **Out-of-order requests**: If CSeq is lower than remote sequence number, reject with 500
3. **To header** = remote URI + remote tag
4. **From header** = local URI + local tag
5. **Route set** determines Request-URI and Route header for subsequent requests

### Target Refresh Requests

A **target refresh request** modifies the remote target URI:

| Request | Effect |
|---------|--------|
| **re-INVITE** | Updates remote target from Contact header |
| **UPDATE** | Updates remote target from Contact header |
| **NOT a target refresh** | ACK, BYE, INFO, PRACK |

### Dialog Lifecycle

```
INVITE ----->         <----- 180 Ringing    (Early dialog)
         ----->        <----- 200 OK         (Confirmed dialog)
         
         <----- BYE ----->                  (Dialog continues)
                   200 OK
         
         <----- BYE ----->                  (Dialog terminated)
                   200 OK
```

---

## 12.3 Termination of a Dialog

### BYE Method

- Sent within dialog to terminate the session
- Does NOT get an ACK (not part of 3-way handshake)
- 200 OK confirms termination

```
BYE sip:bob@192.0.2.4 SIP/2.0
Via: SIP/2.0/UDP pc33.atlanta.com;branch=z9hG4bK776asdhds
From: Alice <sip:alice@atlanta.com>;tag=1928301774
To: Bob <sip:bob@biloxi.com>;tag=a6c85cf
Call-ID: a84b4c76e66710@pc33.atlanta.com
CSeq: 2 BYE
```

### Non-2xx Final Response to INVITE

- Any non-2xx final response (e.g., 486 Busy Here)
- **Terminates early dialogs** immediately

### 481/408 Response to Request

- If UAC receives 481 (Call/Transaction Does Not Exist) or 408 (Request Timeout)
- UAC **SHOULD** terminate the dialog

---

## B2BUA Considerations

For a Back-to-Back User Agent (B2BUA), each call involves **two dialogs**:

```
Caller UA <-----> B2BUA <-----> Callee UA

Dialog 1: Caller <-> B2BUA (Agent Leg)
Dialog 2: B2BUA <-> Callee (Call Leg)
```

### Agent Leg (Incoming Dialog)
- Created when caller sends INVITE to B2BUA
- B2BUA acts as UAS
- Creates dialog from caller's request

### Call Leg (Outgoing Dialog)
- Created when B2BUA sends INVITE to callee
- B2BUA acts as UAC
- Creates dialog to callee's address

### State Management
- B2BUA must coordinate both dialogs
- When one dialog ends, typically end the other
- Can modify SDP in both directions (media anchoring)

---

## Related Sections

- [Messages](03-messages.md) - Request/Response formats
- [Initiating Session](07-initiating-session.md) - INVITE handling
- [Terminating Session](09-terminating-session.md) - BYE handling
- [Transactions](11-transactions.md) - Transaction layer
- [Modifying Session](08-modifying-session.md) - re-INVITE and UPDATE
