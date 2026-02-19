# RFC 3261 - User Agent Behavior

## 8. General User Agent Behavior

SIP User Agents (UAs) can act as both client (UAC) and server (UAS) depending on the message direction.

---

## 8.1 UAC Behavior

### Generating Requests

#### Request-URI (Section 8.1.1.1)

- If UAC has a pre-existing dialog with target: use dialog's remote target
- Otherwise: use address derived from user's input or configuration

#### To Header (Section 8.1.1.2)

- Contains URI of the original request target
- Does NOT get a tag in requests (tag added by UAS in responses)

#### From Header (Section 8.1.1.3)

- Contains URI of the UAC
- **MUST** include a tag parameter for dialog-creating requests
- Tag is a random string that identifies the UAC's "side" of the dialog

#### Call-ID (Section 8.1.1.4)

- **MUST** be a globally unique identifier
- Recommended format: `unique-id@host`
- Used to correlate all messages within a dialog
- UAC is responsible for generating Call-ID

#### CSeq (Section 8.1.1.5)

- Contains a sequence number and method name
- **MUST** be incremented for each new request within a dialog
- Used to order requests and detect retransmissions

#### Max-Forwards (Section 8.1.1.6)

- Integer 0-255
- Decremented by each proxy
- Default: 70
- Prevents request loops

#### Via Header (Section 8.1.1.7)

- **MUST** contain:
  - Transport protocol (UDP, TCP, TLS, SCTP)
  - Host or IP address
  - Port number
  - **Branch parameter** for transaction identification
- Branch parameter **MUST** start with `z9hG4bK` (magic cookie)
- Used to route responses back through the same path

#### Contact (Section 8.1.1.8)

- For INVITE and REGISTER: contains URI where the UA can be reached directly
- For INVITE: used by far end to establish dialog and for subsequent requests

#### Supported and Require (Section 8.1.1.9)

- **Supported**: lists extensions the UAC supports
- **Require**: lists extensions the UAC requires the UAS to understand

### Sending Requests

- UAC sends request to the first hop (proxy or directly to UAS)
- For UDP: may use DNS SRV to locate server
- For reliable transports: no retransmission needed at transport layer

### Processing Responses

#### Transaction Layer Errors

- If transport layer fails: transaction informs TU, may retry via different transport

#### Unrecognized Responses

- Must be treated as a 400 (Bad Request)
- Except 100 (Trying) which is hop-by-hop and may be discarded

#### Via Header Processing

- Check "received" parameter - indicates actual source IP
- Responses are routed based on Via, not Contact

#### Processing 3xx Responses

- Contact header(s) contain alternative addresses
- Can retry with new address automatically
- User intervention typically required

#### Processing 4xx Responses

- Error is permanent or needs user intervention
- Do NOT automatically retry (would get same error)
- May retry with credentials if 401/407

---

## 8.2 UAS Behavior

### Method Inspection

UAS must:
- Process the method in the Request-Line
- Pass to higher layer for processing
- If method not recognized: return 405 (Method Not Allowed)

### Header Inspection

#### To and Request-URI

- Check if Request-URI is recognized
- If not: return 404 (Not Found)

#### Merged Requests

- If request has same Call-ID, To, From, CSeq as existing request but different branch: return 482 (Loop Detected)

#### Require

- Check for required extensions
- If unsupported: return 420 (Bad Extension) with Unsupported header

### Content Processing

- Parse message body if present
- Check Content-Type is supported
- If not: return 415 (Unsupported Media Type)

### Applying Extensions

- Only apply extensions listed in Require header
- If extension requires handling not present: return 420

### Processing the Request

1. Authenticate user (if required)
2. Check authorization
3. Process method
4. Generate response

### Generating Responses

#### Sending Provisional Responses

- 1xx responses are **hop-by-hop** (not proxied through Record-Route)
- Can be sent multiple times (retransmitted)
- **Must NOT** be sent reliably unless UAC requires it (100rel)

#### Headers and Tags

- **Must** add To tag if not present
- Tags are critical for dialog identification

### Stateless UAS Behavior

Some UAS (like proxies) process requests without creating transaction state:
- Process request
- Generate response
- No retransmission handling

---

## 9. Canceling a Request

### 9.1 UAC Behavior

The CANCEL request is used to cancel a pending request that has not yet received a final response.

#### Rules for CANCEL

- CANCEL **must** have same:
  - Request-URI
  - To, From, Call-ID
  - CSeq (method=CANCEL, number=original request's CSeq number)
  - Via (exact copy, including branch)
- **Must NOT** be sent for requests that already received final response
- UAC should wait for final response to original request after sending CANCEL

### 9.2 Server Behavior

#### Proxy/Registrar

- If has transaction state for original: send CANCEL to client
- CANCEL is processed at transaction layer

#### UAS

- If received CANCEL for request in progress: stop processing
- Return 487 (Request Terminated) for original request
- **Must NOT** send 200 OK to CANCEL

---

## Related Sections

- [Messages](03-messages.md) - Request/Response formats
- [Dialogs](06-dialogs.md) - Dialog creation
- [Transactions](11-transactions.md) - Transaction handling
- [Headers](13-headers.md) - Header field details
- [Response Codes](14-response-codes.md) - Response code meanings
