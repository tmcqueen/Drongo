# RFC 3261 - Header Fields

## 20. Header Fields

Header fields provide metadata about the SIP message. This section details key headers for implementation.

---

## Essential Headers

### Call-ID

**Purpose:** Unique identifier for a call/dialog

**Format:** `Call-ID: unique-id@host`

**Rules:**
- Globally unique
- Case-sensitive
- Must match for all requests/responses in a dialog

**Compact Form:** `i`

---

### CSeq (Command Sequence)

**Purpose:** Sequence number for ordering requests within a dialog

**Format:** `CSeq: sequence-number METHOD`

**Rules:**
- Must be strictly increasing within a dialog
- Used to detect retransmissions
- Examples: `CSeq: 314159 INVITE`, `CSeq: 1 REGISTER`

---

### From

**Purpose:** Originator of the request

**Format:** `From: display-name <SIP-URI>;tag=tag-value`

**Example:** `From: Alice <sip:alice@atlanta.com>;tag=1928301774`

**Rules:**
- Must include tag parameter for dialog-creating requests
- Tag identifies sender's "side" of dialog

**Compact Form:** `f`

---

### To

**Purpose:** Destination of the request

**Format:** `To: display-name <SIP-URI>;tag=tag-value`

**Example:** `To: Bob <sip:bob@biloxi.com>`

**Rules:**
- Does NOT have tag in requests (added by UAS in responses)
- Tag added by UAS to create dialog

**Compact Form:** `t`

---

### Via

**Purpose:** Transport path taken by the request

**Format:** `Via: SIP/2.0/TRANSPORT sent-by;branch=branch-value`

**Example:** `Via: SIP/2.0/UDP pc33.atlanta.com;branch=z9hG4bK776asdhds`

**Components:**
| Component | Description |
|-----------|-------------|
| SIP/2.0 | Protocol version |
| TRANSPORT | UDP, TCP, TLS, SCTP |
| sent-by | Host and port |
| branch | Transaction ID (must start with `z9hG4bK`) |
| received | Added by proxy with actual source IP |
| rport | Added for RFC 3581 NAT handling |

**Compact Form:** `v`

---

### Contact

**Purpose:** Direct URI for subsequent requests

**Format:** `Contact: <SIP-URI>;q=0.8;expires=3600`

**Example:** `Contact: <sip:alice@pc33.atlanta.com>`

**Rules:**
- Required for INVITE and REGISTER
- Contains URI where UA can be reached directly
- Used for dialog establishment and subsequent requests

**Parameters:**
| Parameter | Description |
|-----------|-------------|
| q | Priority (0.0-1.0, higher = preferred) |
| expires | Expiration time in seconds |
|Methods | Supported methods (e.g., "*" for all) |

**Compact Form:** `m`

---

### Max-Forwards

**Purpose:** Limit number of hops

**Format:** `Max-Forwards: 70`

**Rules:**
- Integer 0-255
- Decremented by each proxy
- If reaches 0: 483 (Too Many Hops) returned

---

### Record-Route

**Purpose:** Force proxy to stay in path

**Format:** `Record-Route: <SIP-URI>;parameter`

**Example:** `Record-Route: <sip:proxy.atlanta.com;lr>`

**Rules:**
- Inserted by proxies wanting to remain in call path
- Preserved by UAs in reverse order
- "lr" parameter indicates loose routing

---

### Route

**Purpose:** Explicit routing path

**Format:** `Route: <SIP-URI>`

**Rules:**
- Used with loose routing
- Processed in order
- Request-URI can also be a route target

---

### Require

**Purpose:** List required extensions

**Format:** `Require: option-tag1,option-tag2`

**Example:** `Require: 100rel`

**Rules:**
- UAS must understand all listed extensions
- If unsupported: 420 (Bad Extension)

---

### Supported

**Purpose:** List supported extensions

**Format:** `Supported: option-tag1,option-tag2`

**Example:** `Supported: 100rel,timer`

**Rules:**
- Optional to include
- Helps negotiation

**Compact Form:** `k`

---

### Content-Length

**Purpose:** Size of message body

**Format:** `Content-Length: 142`

**Rules:**
- Essential for TCP/TLS to determine message boundaries
- Default: 0

**Compact Form:** `l`

---

### Content-Type

**Purpose:** Type of message body

**Format:** `Content-Type: type/subtype`

**Example:** `Content-Type: application/sdp`

**Rules:**
- Required if message has body
- Common: `application/sdp`, `application/isup`

**Compact Form:** `c`

---

### Expires

**Purpose:** Expiration time

**Format:** `Expires: 3600`

**Rules:**
- Seconds until expiration
- Used in REGISTER, INVITE responses

---

### Authorization / Proxy-Authorization

**Purpose:** Credentials for authentication

**Format:** `Authorization: Digest username="...", realm="...", ...`

**Rules:**
- Contains credentials for digest authentication
- Used in 401/407 challenges

---

## Compact Form Summary

| Full | Compact |
|------|---------|
| Call-ID | i |
| Contact | m |
| Content-Encoding | e |
| Content-Length | l |
| Content-Type | c |
| From | f |
| Record-Route | r |
| Require | r |
| Supported | k |
| To | t |
| Via | v |

---

## Header Processing Rules

1. Order doesn't matter for most headers
2. Multi-value headers can be comma-separated
3. Required headers: To, From, Call-ID, CSeq, Max-Forwards, Via
4. Contact required for: INVITE, REGISTER, 200 OK to INVITE
5. All headers are case-insensitive except values noted otherwise

---

## Related Sections

- [Messages](03-messages.md) - Message format
- [Dialogs](06-dialogs.md) - How headers create dialogs
- [Transactions](11-transactions.md) - Via for transaction matching
- [Response Codes](14-response-codes.md) - Response codes
