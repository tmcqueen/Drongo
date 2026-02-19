# RFC 3261 - Proxy Behavior

## 16. Proxy Behavior

A **Proxy Server** is an intermediate entity that forwards requests on behalf of the requestor. Unlike a B2BUA, a proxy does not terminate dialogs or manage sessions.

### Proxy Types

| Type | Description |
|------|-------------|
| **Stateful Proxy** | Maintains transaction state, handles retransmissions |
| **Stateless Proxy** | No state, just forwards each request/response |

---

## 16.1 Overview

### Basic Proxy Function

1. Receive request
2. Process headers
3. Determine targets
4. Forward request
5. Receive responses
6. Forward responses back

```
Caller ----> Proxy ----> Callee
   |                  |
   |<---- Proxy <-----|
```

---

## 16.2 Stateful Proxy

A stateful proxy maintains transaction state for both requests and responses.

### Characteristics

- Creates server transactions for incoming requests
- Creates client transactions for forwarded requests
- Handles retransmissions
- Handles timeout responses

---

## 16.3 Request Validation

Proxies MUST validate requests:

1. **Syntax**: Well-formed SIP message
2. **URI scheme**: Request-URI is SIP or SIPS
3. **Max-Forwards**: Not zero
4. **Loop detection**: Hasn't seen this request before (via branch)
5. **Proxy-Require**: Supports required extensions

If validation fails: return error response

---

## 16.4 Route Information Preprocessing

### Loose Routing vs Strict Routing

- **Loose Routing**: Proxy uses Route header to determine next hop, doesn't modify Request-URI
- **Strict Routing**: Proxy modifies Request-URI to first Route header

### Record-Route

Proxies can insert Record-Route to remain in the path for subsequent requests:

```
Record-Route: <sip:proxy1.atlanta.com;lr>
Record-Route: <sip:proxy2.biloxi.com;lr>
```

### Processing

1. If Request-URI is a loose route (has lr parameter): use as-is
2. If Request-URI is not a Route: prepend Route set
3. Process Route headers in order

---

## 16.5 Determining Request Targets

### Normal Request Processing

1. **Pre-existing dialog**: If dialog exists, use remote target
2. **Location service**: Look up Request-URI in database
3. **Multiple targets**: Can fork (send to multiple)

### Target Discovery

- If Request-URI is AoR: query location service
- If Request-URI is contact: use directly
- Contact can come from:
  - REGISTER bindings
  - DNS NAPTR/SRV records

---

## 16.6 Request Forwarding

### Steps

1. **Add Via header** with own address
2. **Decrement Max-Forwards**
3. **Add Record-Route** if staying in path
4. **Route request** to target(s)
5. **Set timer** to wait for response

### Forwarding Example

```
INVITE sip:bob@biloxi.com SIP/2.0
Via: SIP/2.0/UDP proxy.atlanta.com;branch=z9hG4bKnew1
Max-Forwards: 69
Record-Route: <sip:proxy.atlanta.com;lr>
...

-> forwarded as:

INVITE sip:bob@192.0.2.4 SIP/2.0
Via: SIP/2.0/UDP proxy.atlanta.com;branch=z9hG4bKnew1
Via: SIP/2.0/UDP pc33.atlanta.com;branch=z9hG4bK776asdhds
Max-Forwards: 69
Record-Route: <sip:proxy.atlanta.com;lr>
...
```

---

## 16.7 Response Processing

### Stateless Proxy

- Forward responses based on Via header
- Remove top Via, forward to next address

### Stateful Proxy

- Match response to server transaction
- Match response to client transaction
- Forward through client transaction
- Client transaction forwards to server transaction

### Response Routing

```
Response comes back:
  - Proxy removes own Via
  - Looks at next Via
  - Sends to address in Via's "received" parameter
```

---

## 16.8 Processing Timer C

For stateful proxy processing INVITE:
- Timer C monitors INVITE transaction
- Default: 3 minutes (configurable)
- If expires: terminate transaction, return 408

---

## 16.9 Handling Transport Errors

- If UDP send fails: switch to TCP
- If TCP connection fails: try again
- Return 503 if cannot deliver

---

## 16.10 CANCEL Processing

### Proxy Behavior

- If has transaction state for original request
- And request hasn't received final response
- Forward CANCEL to same targets

### Rules

- CANCEL to each destination original request was sent to
- Only affects pending requests

---

## 16.11 Stateless Proxy

### Characteristics

- No transaction state
- Each request processed independently
- Cannot do parallel forking
- Cannot do authentication

### Processing

1. Validate request
2. Determine targets
3. Forward to each target
4. Forward responses back

---

## 16.12 Summary of Proxy Route Processing

```
1. Validate request
2. Preprocess Route headers
3. Determine target(s)
4. Add Via, Record-Route, decrement Max-Forwards
5. Forward request
6. Receive response
7. Remove own Via, forward response
```

---

## Key Differences: Proxy vs B2BUA

| Feature | Proxy | B2BUA |
|---------|-------|-------|
| Dialog state | None | Both legs |
| Media | No | Yes (termination) |
| SDP modification | No | Yes |
| Record-Route | Stays in path | Can strip |
| Call routing | Forward | Initiate new |

---

## Related Sections

- [Dialogs](06-dialogs.md) - Dialog creation
- [Transactions](11-transactions.md) - Transaction handling
- [Transport](12-transport.md) - Transport considerations
- [Messages](03-messages.md) - Headers
