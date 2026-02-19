# RFC 3261 - Security Considerations

## 26. Security Considerations

---

## 26.1 Attacks and Threat Models

### 26.1.1 Registration Hijacking

**Threat:** Attacker registers false contact for user's AOR

**Impact:** Calls routed to attacker instead of legitimate user

**Mitigation:**
- Authentication of REGISTER requests
- TLS for transport
- SIPS URIs

---

### 26.1.2 Impersonating a Server

**Threat:** Attacker poses as proxy or registrar

**Impact:** Man-in-the-middle, call interception

**Mitigation:**
- Server authentication
- TLS with certificates
- SIPS URIs

---

### 26.1.3 Tampering with Message Bodies

**Threat:** Modify SDP to change media parameters

**Impact:**
- Redirect media to attacker
- Remove encryption
- Change codec

**Mitigation:**
- Message integrity (S/MIME)
- TLS transport

---

### 26.1.4 Tearing Down Sessions

**Threat:** Send forged BYE to terminate call

**Impact:** Premature session termination

**Mitigation:**
- Authentication of BYE
- Dialog validation (tags, Call-ID)

---

### 26.1.5 Denial of Service and Amplification

**Threat:** 
- Flood with requests
- Amplify responses (small request → large response)

**Impact:** Service unavailable

**Mitigation:**
- Rate limiting
- Authentication
- Ingress filtering

---

## 26.2 Security Mechanisms

### 26.2.1 Transport and Network Layer Security

#### TLS (Transport Layer Security)

- Encrypts SIP messages
- Authenticates servers
- Can provide mutual authentication

```
sips:bob@biloxi.com  (TLS required)
sip:bob@biloxi.com   (TLS optional)
```

#### IPsec

- Network-level encryption
- Can protect all SIP traffic

---

### 26.2.2 SIPS URI Scheme

- Requires TLS for entire path
- End-to-end security
- Certificate validation required

---

### 26.2.3 HTTP Authentication

**Digest Authentication:**
- Challenge-response mechanism
- Password-based
- Used with 401 (Unauthorized) and 407 (Proxy Auth Required)

**Process:**
```
UAS: 401 Unauthorized
    WWW-Authenticate: Digest realm="biloxi.com", qop="auth"

UAC: Authorization: Digest username="bob", ...
```

---

### 26.2.4 S/MIME

- End-to-end integrity and confidentiality
- Encrypts message bodies (SDP)
- Signs headers
- Complex to implement

---

## 26.3 Implementing Security

### Requirements for Implementers

1. **MUST** support Digest authentication
2. **SHOULD** support TLS
3. **SHOULD** validate certificates
4. **SHOULD** implement authentication for:
   - INVITE (to prevent call hijacking)
   - BYE (to prevent call termination)
   - REGISTER (to prevent registration hijacking)

---

## 26.4 Limitations

### HTTP Digest

- No encryption, only authentication
- Vulnerable to replay attacks
- Weak passwords vulnerable to offline attacks
- Should use TLS

### S/MIME

- Complex key management
- Not widely deployed
- Certificate infrastructure required

### TLS

- Only hop-by-hop, not end-to-end
- Requires PKI
- Certificate validation can be problematic

---

## Recommendations

### Minimum Security

1. **Authentication**: Digest authentication on all requests
2. **Transport**: TLS for production
3. **Validation**: Verify dialog identifiers (Call-ID, tags)

### Recommended Security

1. **End-to-end**: SIPS URIs
2. **Message integrity**: S/MIME for SDP
3. **Certificate management**: Proper PKI

---

## Related Sections

- [Messages](03-messages.md) - Authorization headers
- [Dialogs](06-dialogs.md) - Dialog security
- [Transport](12-transport.md) - TLS transport
- [Authentication](22-authentication.md) - Digest authentication details
