# RFC 3261 - SIP Messages

## Overview

SIP messages are text-based and follow RFC 2822 format. Messages consist of a start-line, header fields, an empty line (CRLF), and an optional message body.

```
generic-message = start-line
                  *message-header
                  CRLF
                  [message-body]
```

---

## 7.1 Requests

### Request-Line Format

```
Request-Line = Method SP Request-URI SP SIP-Version CRLF
```

Example:
```
INVITE sip:bob@biloxi.com SIP/2.0
```

### SIP Methods

| Method | Description | Section |
|--------|-------------|---------|
| **INVITE** | Initiate a session | 13 |
| **ACK** | Confirm final response to INVITE | 13 |
| **BYE** | Terminate a session | 15 |
| **CANCEL** | Cancel pending request | 9 |
| **OPTIONS** | Query capabilities | 11 |
| **REGISTER** | Register location | 10 |

### Request-URI

The Request-URI is the address being requested:
- Typically the AOR of the called party
- Can be a SIP or SIPS URI
- For direct calls, contains the target's contact address

---

## 7.2 Responses

### Status-Line Format

```
Status-Line = SIP-Version SP Status-Code SP Reason-Phrase CRLF
```

Example:
```
SIP/2.0 200 OK
```

### Response Code Classes

| Code Range | Class | Meaning |
|------------|-------|---------|
| **1xx** | Provisional | Request received, continuing to process |
| **2xx** | Success | Action successfully received, understood, accepted |
| **3xx** | Redirection | Further action must be taken to complete request |
| **4xx** | Client Error | Request contains bad syntax or cannot be fulfilled |
| **5xx** | Server Error | Server failed to fulfill a valid request |
| **6xx** | Global Failure | Request cannot be fulfilled at any server |

---

## 7.3 Header Fields

### Header Field Format

```
field-name: field-value
```

Examples:
```
Via: SIP/2.0/UDP pc33.atlanta.com;branch=z9hG4bK776asdhds
From: Alice <sip:alice@atlanta.com>;tag=1928301774
To: Bob <sip:bob@biloxi.com>
Call-ID: a84b4c76e66710@pc33.atlanta.com
```

### Header Classification

Headers can be:
- **Required**: Must be present in every request
- **Optional**: May be present
- **Entity**: Describe the message body

### Compact Form

Some headers have compact forms for bandwidth efficiency:

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

## 7.4 Bodies

### Content-Type

The message body type is indicated by the `Content-Type` header. Common types:
- `application/sdp` - Session Description Protocol
- `application/isup` - ISUP (for PSTN interop)

### Content-Length

Indicates the size of the message body in octets. Essential for TCP and TLS transports to determine message boundaries.

---

## Required Headers

### For All Requests

| Header | Purpose |
|--------|---------|
| **To** | Destination address (URI) |
| **From** | Source address (URI) |
| **Call-ID** | Unique call identifier |
| **CSeq** | Command sequence (method + number) |
| **Max-Forwards** | Maximum number of hops (default 70) |
| **Via** | Transport, address, port, branch |

### Additional for INVITE/REGISTER

| Header | Purpose |
|--------|---------|
| **Contact** | Direct URI for subsequent requests |

---

## Example INVITE Request

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

(SDP body here)
```

---

## Example 200 OK Response

```
SIP/2.0 200 OK
Via: SIP/2.0/UDP server10.biloxi.com;branch=z9hG4bKnashds8;received=192.0.2.3
Via: SIP/2.0/UDP bigbox3.site3.atlanta.com;branch=z9hG4bK77ef4c2312983.1;received=192.0.2.2
Via: SIP/2.0/UDP pc33.atlanta.com;branch=z9hG4bK776asdhds;received=192.0.2.1
To: Bob <sip:bob@biloxi.com>;tag=a6c85cf
From: Alice <sip:alice@atlanta.com>;tag=1928301774
Call-ID: a84b4c76e66710@pc33.atlanta.com
CSeq: 314159 INVITE
Contact: <sip:bob@192.0.2.4>
Content-Type: application/sdp
Content-Length: 131

(SDP body here)
```

---

## Related Sections

- [Protocol Overview](02-protocol-overview.md) - Structure of the protocol
- [Dialogs](06-dialogs.md) - How dialogs use these headers
- [Transactions](11-transactions.md) - Transaction matching using Via
- [Headers](13-headers.md) - Detailed header field reference
- [Response Codes](14-response-codes.md) - All response codes
