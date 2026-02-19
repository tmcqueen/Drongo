# RFC 3261 - Transport

## 18. Transport

The Transport layer handles how SIP messages are sent and received over the network.

### Supported Transport Protocols

| Protocol | Reliable | Secure | Notes |
|----------|----------|--------|-------|
| **UDP** | No | No | Default for RFC 3261 |
| **TCP** | Yes | No | Connection-oriented |
| **TLS** | Yes | Yes | Encrypted |
| **SCTP** | Yes | No | Multi-streaming |

---

## 18.1 Clients

### 18.1.1 Sending Requests

#### Choosing Transport

1. If Request-URI is SIPS: use TLS
2. If loose source routing: use transport from first route
3. Otherwise: use UDP, or try TCP

#### Response Destination

- Send response to address in Via header's "received" parameter
- If no "received": use source address of request
- Use port from Via (or default 5060)

#### Via Header

- Client adds Via header with:
  - Transport (UDP/TCP/TLS)
  - Sent-by (host:port)
  - Branch parameter

### 18.1.2 Receiving Responses

- Match response to transaction using Via branch + sent-by
- Pass to transaction layer
- Discard duplicates (transaction handles)

---

## 18.2 Servers

### 18.2.1 Receiving Requests

1. Parse message
2. Validate required headers
3. Check transport matches Via
4. Add "received" parameter to Via with source IP
5. Route to transaction layer

### 18.2.2 Sending Responses

1. Check Via for destination
2. Send to address in "received" parameter
3. Use port from Via

---

## 18.3 Framing

### UDP

- Messages are self-delimited by Content-Length
- Multiple messages can be in single datagram

### TCP/TLS

- Messages use Content-Length to determine boundaries
- Stream can contain multiple messages

Example:
```
INVITE sip:bob@biloxi.com SIP/2.0
Content-Length: 400

<400 bytes>
INVITE sip:alice@atlanta.com SIP/2.0
Content-Length: 400

<400 bytes>
```

---

## 18.4 Error Handling

### UDP

- No connection, just send
- Errors typically cause ICMP (may be ignored)

### TCP/TLS

- Connection errors must be reported
- May retry with different connection

---

## Via Header Branch Parameter

### Magic Cookie

Branch parameter **MUST** start with `z9hG4bK` for transaction identification:

```
Via: SIP/2.0/UDP pc33.atlanta.com;branch=z9hG4bK776asdhds
```

This allows:
- Transaction matching
- Loop detection
- Response routing

---

## Reliable Transport Considerations

### UDP → TCP Fallback

If UDP fails:
- Client can switch to TCP
- Must use same transaction key

### Keep-Alive

- RFC 3261 recommends CRLF keep-alive
- Prevents NAT/firewall timeouts
- Optional for UDP

```
CRLF
CRLF
```

---

## Related Sections

- [Messages](03-messages.md) - Via header, Content-Length
- [Transactions](11-transactions.md) - Transaction matching
- [Security](15-security.md) - TLS transport
