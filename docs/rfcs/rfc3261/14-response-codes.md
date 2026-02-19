# RFC 3261 - Response Codes

## 21. Response Codes

SIP responses consist of a three-digit status code and a reason phrase.

---

## 1xx - Provisional Responses

Informational responses indicating request is being processed.

| Code | Name | Description |
|------|------|-------------|
| **100** | Trying | Request received, locating destination |
| **180** | Ringing | UA is alerting user |
| **181** | Call Is Being Forwarded | Call is being forwarded |
| **182** | Queued | Temporarily unavailable, queued |
| **183** | Session Progress | Progress information, may include early media |

---

## 2xx - Success Responses

Request was successfully received, understood, and accepted.

| Code | Name | Description |
|------|------|-------------|
| **200** | OK | Request succeeded |
| **202** | Accepted | Request accepted for processing (extensions) |
| **204** | No Notification | Request succeeded but no notification will be sent |

---

## 3xx - Redirection Responses

Further action needed to complete request.

| Code | Name | Description |
|------|------|-------------|
| **300** | Multiple Choices | Several locations available |
| **301** | Moved Permanently | New location is permanent |
| **302** | Moved Temporarily | New location is temporary |
| **305** | Use Proxy | Must use specified proxy |
| **380** | Alternative Service | Call failed, alternatives exist |

---

## 4xx - Client Error Responses

Request has bad syntax or cannot be fulfilled at this server.

| Code | Name | Description |
|------|------|-------------|
| **400** | Bad Request | Malformed syntax |
| **401** | Unauthorized | Authentication required |
| **402** | Payment Required | Reserved for future use |
| **403** | Forbidden | Will not fulfill, auth won't help |
| **404** | Not Found | User does not exist |
| **405** | Method Not Allowed | Method not supported |
| **406** | Not Acceptable | Content not acceptable |
| **407** | Proxy Authentication Required | Proxy authentication needed |
| **408** | Request Timeout | No response in time |
| **410** | Gone | Resource no longer available |
| **413** | Request Entity Too Large | Message body too large |
| **414** | Request-URI Too Long | URI too long |
| **415** | Unsupported Media Type | Body format not supported |
| **416** | Unsupported URI Scheme | URI scheme not supported |
| **420** | Bad Extension | Extension not understood |
| **421** | Extension Required | Specific extension needed |
| **423** | Interval Too Brief | Expiration time too short |
| **480** | Temporarily Unavailable | User not currently available |
| **481** | Call/Transaction Does Not Exist | No matching dialog/transaction |
| **482** | Loop Detected | Loop in routing |
| **483** | Too Many Hops | Max-Forwards reached 0 |
| **484** | Address Incomplete | Incomplete dialing string |
| **485** | Ambiguous | Request-URI ambiguous |
| **486** | Busy Here | User is busy |
| **487** | Request Terminated | Request was cancelled |
| **488** | Not Acceptable Here | Media not acceptable |
| **491** | Request Pending | Request already pending |
| **493** | Undecipherable | Cannot decrypt body |

---

## 5xx - Server Error Responses

Server failed to fulfill a valid request.

| Code | Name | Description |
|------|------|-------------|
| **500** | Server Internal Error | Unexpected condition |
| **501** | Not Implemented | Method not supported by server |
| **502** | Bad Gateway | Invalid response from downstream server |
| **503** | Service Unavailable | Temporarily overloaded |
| **504** | Server Time-out | Timed out waiting for external server |
| **505** | Version Not Supported | SIP version not supported |
| **513** | Message Too Large | Message exceeds capabilities |

---

## 6xx - Global Failure Responses

Request cannot be fulfilled at any server.

| Code | Name | Description |
|------|------|-------------|
| **600** | Busy Everywhere | User is busy everywhere |
| **603** | Decline | User declined the request |
| **604** | Does Not Exist Anywhere | User does not exist |
| **606** | Not Acceptable | Session not acceptable globally |

---

## Response Code Usage

### INVITE

| Response | Meaning | Action |
|----------|---------|--------|
| 100 | Trying | Optional, stop retransmissions |
| 180 | Ringing | User alerted |
| 183 | Session Progress | Early media possible |
| 200 | OK | Session established |
| 3xx | Redirect | Try new location |
| 4xx | Client Error | Don't retry same server |
| 5xx | Server Error | May retry different server |
| 6xx | Global | Won't succeed anywhere |

### REGISTER

| Response | Meaning |
|----------|---------|
| 200 | Registration successful |
| 400 | Bad request |
| 401/407 | Authentication required |
| 423 | Expiration too brief |

### BYE/CANCEL

| Response | Meaning |
|----------|---------|
| 200 | OK |
| 481 | Call/Transaction doesn't exist |

---

## Related Sections

- [Messages](03-messages.md) - Response format
- [Initiating Session](07-initiating-session.md) - INVITE responses
- [Dialogs](06-dialogs.md) - Dialog termination
- [Transactions](11-transactions.md) - Transaction handling
