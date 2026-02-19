# RFC 3261 - Modifying a Session

## 14. Modifying an Existing Session

During an established session (dialog), either party can modify the session parameters using:
- **re-INVITE**: Modifies session (new SDP offer)
- **UPDATE**: Modifies session without affecting dialog state

Both are **target refresh requests** - they can update the remote target URI.

---

## 14.1 UAC Behavior

### re-INVITE

A re-INVITE is sent within an established dialog to:
- Change media parameters (codec, port, etc.)
- Add or remove media streams
- Hold or resume a call
- Transfer the call

#### Requirements

- Must be within an established dialog
- Must have same Call-ID
- Must have local and remote tags
- CSeq must be higher than previous
- Request-URI = remote target from dialog
- Route set determines routing

#### Example re-INVITE

```
INVITE sip:bob@192.0.2.4 SIP/2.0
Via: SIP/2.0/UDP pc33.atlanta.com;branch=z9hG4bK776asdhds
From: Alice <sip:alice@atlanta.com>;tag=1928301774
To: Bob <sip:bob@biloxi.com>;tag=a6c85cf
Call-ID: a84b4c76e66710@pc33.atlanta.com
CSeq: 314160 INVITE
Contact: <sip:alice@pc33.atlanta.com>
Route: <sip:bob@192.0.2.4>
Content-Type: application/sdp
Content-Length: 142

(new SDP offer)
```

### Processing Responses to re-INVITE

- **1xx**: Update remote target if Contact present
- **2xx**: 
  - Update dialog state (remote target, session parameters)
  - Send ACK
  - Session modified
- **3xx-6xx**:
  - Send ACK
  - **Original session continues unchanged**

---

## 14.2 UAS Behavior

### Processing re-INVITE

1. **Check dialog**: Must match existing dialog
2. **Check CSeq**: Must be higher than previous
3. **Process SDP**: Generate answer
4. **Update state**: Update remote target if Contact present
5. **Send response**: 200 OK, or error

### Responds to re-INVITE

| Response | Action |
|----------|--------|
| **200 OK** | Accept modification, send SDP answer |
| **488 Not Acceptable Here** | Reject modification, session unchanged |
| **491 Request Pending** | Cannot process, try later |
| **500** | Server error |

### ACK to re-INVITE

- UAS receives ACK directly to Contact
- Same behavior as initial INVITE

---

## UPDATE Method (RFC 3311)

The UPDATE method allows session modification without affecting the dialog state.

### Differences from re-INVITE

| Feature | re-INVITE | UPDATE |
|---------|-----------|--------|
| Dialog state | May change | Does not change |
| Can be sent early | No (dialog needed) | Yes (after offer/answer) |
| Can replace SDP | Yes | Yes |
| Changes dialog | Maybe | No |

### Usage

- Can be used for mid-dialog capability changes
- Often used for hold/resume
- Simpler than re-INVITE for some use cases

---

## Key Rules for Session Modification

1. **Atomic**: Either entire modification succeeds or fails
2. **Original session persists**: If re-INVITE fails, original session continues
3. **No SDP**: If re-INVITE has no SDP, UAS generates offer in response
4. **Target refresh**: Both re-INVITE and UPDATE can update remote target
5. **CSeq ordering**: Must be strictly increasing

---

## Related Sections

- [Initiating Session](07-initiating-session.md) - Initial INVITE
- [Terminating Session](09-terminating-session.md) - BYE
- [Dialogs](06-dialogs.md) - Dialog state
- [Transactions](11-transactions.md) - Non-INVITE transaction
