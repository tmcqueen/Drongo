# RFC 3261 - Registrations

## 10. Registrations

### Overview

The REGISTER request binds a user's Address-of-Record (AOR) to one or more contact addresses. This allows proxy servers and registrars to locate the user for incoming calls.

### Key Concepts

- **Registrar**: Server that processes REGISTER requests and maintains a location database
- **Binding**: Association between AOR and contact address
- **AOR**: SIP URI that points to a domain with a location service (e.g., `sip:bob@biloxi.com`)
- **Contact**: URI where the user can be reached

---

## 10.2 Constructing REGISTER Requests

### Required Headers

| Header | Purpose |
|--------|---------|
| **To** | The AOR being registered (e.g., `sip:bob@biloxi.com`) |
| **From** | The UA making the registration (often same as To) |
| **Call-ID** | Unique identifier for this registration session |
| **CSeq** | Sequence number for this REGISTER request |
| **Contact** | The address(es) to bind to the AOR |
| **Expires** | Time in seconds until registration expires |

### Contact Header

The Contact header contains the address(es) to register:

```
Contact: <sip:bob@192.0.2.4:5060>;expires=3600
Contact: <sip:bob@192.0.2.5:5060>;q=0.8
```

Parameters:
- **expires**: Time until binding expires (default: 3600 seconds / 1 hour)
- **q**: Priority (0.0 to 1.0, higher = more preferred)

### Example REGISTER Request

```
REGISTER sip:biloxi.com SIP/2.0
Via: SIP/2.0/UDP pc33.atlanta.com;branch=z9hG4bK776asdhds
Max-Forwards: 70
To: Bob <sip:bob@biloxi.com>
From: Bob <sip:bob@biloxi.com>;tag=9388j234
Call-ID: 84317@pc33.atlanta.com
CSeq: 1 REGISTER
Contact: <sip:bob@192.0.2.4:5060>
Expires: 3600
```

---

## 10.2.1 Adding Bindings

When a Registrar receives a REGISTER with a Contact header:

1. **New registration**: Add binding to location service
2. **Refresh**: Update expiration time for existing binding
3. **Replacement**: Replace all bindings if `q` parameter is present

### Setting Expiration Interval

- Contact with `expires` parameter uses that value
- If no expires: use Expires header value
- If neither: default to 3600

### Preferences Among Contact Addresses

- Use `q` parameter (0.0 to 1.0)
- Higher `q` = higher priority
- If no q: treat as q=1.0

---

## 10.2.2 Removing Bindings

To remove a binding:
- Send REGISTER with Contact containing `expires=0`
- Or let registration expire naturally

```
Contact: <sip:bob@192.0.2.4:5060>;expires=0
```

---

## 10.2.3 Fetching Bindings

To query bindings:
- REGISTER with Contact: *
- Registrar returns all current bindings

---

## 10.2.4 Refreshing Bindings

To keep a registration active:
- Send same REGISTER before expiration
- Update Expires value as needed

---

## 10.3 Processing REGISTER Requests

### Registrar Processing

1. **Extract AOR** from To header
2. **Authenticate** user (if required)
3. **Process Contact headers**:
   - Add new bindings
   - Update existing bindings
   - Remove expired/zero-expire bindings
4. **Return response** with current bindings

### Example 200 OK Response

```
SIP/2.0 200 OK
Via: SIP/2.0/UDP pc33.atlanta.com;branch=z9hG4bK776asdhds
To: Bob <sip:bob@biloxi.com>;tag=9388j234
From: Bob <sip:bob@biloxi.com>;tag=9388j234
Call-ID: 84317@pc33.atlanta.com
CSeq: 1 REGISTER
Contact: <sip:bob@192.0.2.4:5060>;expires=3600
Contact: <sip:bob@192.0.2.5:5060>;q=0.8;expires=3600
Expires: 3600
```

---

## Key Rules for Implementation

1. **Contact matching**: Match contacts by comparison rules (exact match or glob *)
2. **Expiration limits**: Registrar may enforce minimum/maximum expires
3. **Wildcards**: Contact: * removes all bindings (requires authentication)
4. **Multiple contacts**: Return in priority order (q value)
5. **Stale bindings**: Remove automatically after expiration

---

## Authentication

REGISTER requests typically require authentication:

- **Digest authentication** (RFC 2617)
- 401 (Unauthorized) to request credentials
- 407 (Proxy Authentication Required) for proxy authentication

---

## Related Sections

- [Messages](03-messages.md) - Request format
- [User Agent Behavior](04-user-agent-behavior.md) - UAC/UAS behavior
- [Dialogs](06-dialogs.md) - How registrations relate to dialogs
- [Authentication](22-authentication.md) - Digest authentication
