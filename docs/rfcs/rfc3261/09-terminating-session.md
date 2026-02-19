# RFC 3261 - Terminating a Session

## 15. Terminating a Session

Sessions are terminated using the BYE request. Unlike INVITE, BYE follows a simple 2-way handshake.

---

## 15.1 BYE Request

### Purpose

The BYE request terminates an established session and the corresponding dialog.

### Key Characteristics

- **Sent within a dialog**
- **Does NOT get an ACK** (not part of 3-way handshake like INVITE)
- Follows normal transaction processing
- **Cannot be sent for early dialogs** (use 3xx/487 response instead)

### Requirements

- Request-URI = remote target from dialog
- To = remote URI + remote tag
- From = local URI + local tag
- Call-ID = dialog's Call-ID
- CSeq = sequence number with method BYE

### Example BYE

```
BYE sip:bob@192.0.2.4 SIP/2.0
Via: SIP/2.0/UDP pc33.atlanta.com;branch=z9hG4bK776asdhds
From: Alice <sip:alice@atlanta.com>;tag=1928301774
To: Bob <sip:bob@biloxi.com>;tag=a6c85cf
Call-ID: a84b4c76e66710@pc33.atlanta.com
CSeq: 314161 BYE
```

---

## 15.1.1 UAC Behavior

### Sending BYE

1. Create BYE request within dialog
2. Send using dialog's route set and remote target
3. Wait for 200 OK response

### After Sending BYE

- **Must NOT** send further requests in dialog after BYE
- **Must NOT** send ACK for 200 OK (not part of transaction)

### Processing Responses

- Any 2xx response confirms termination
- 481 indicates dialog doesn't exist (already terminated)
- 408 indicates no response (dialog may still exist)

---

## 15.1.2 UAS Behavior

### Processing BYE

1. Verify dialog exists
2. Verify request is from authenticated user (if required)
3. Stop any media processing
4. Terminate session

### Sending Response

- Send 200 OK
- Include appropriate headers

### 200 OK to BYE

```
SIP/2.0 200 OK
Via: SIP/2.0/UDP pc33.atlanta.com;branch=z9hG4bK776asdhds
From: Alice <sip:alice@atlanta.com>;tag=1928301774
To: Bob <sip:bob@biloxi.com>;tag=a6c85cf
Call-ID: a84b4c76e66710@pc33.atlanta.com
CSeq: 314161 BYE
```

---

## 15.2 Session Termination Flow

```
Alice                                              Bob
   |                                                 |
   |================= Established Session ===========|
   |                                                 |
   |--- BYE --------------------------------------->|
   |<-- 200 OK -------------------------------------|
   |                                                 |
   |                  (Session Terminated)           |
```

---

## Non-2xx Final Response to INVITE

Early dialogs (created by 1xx responses) are terminated by any non-2xx final response:

```
UAC                                              UAS
   |                                                 |
   |------------------- INVITE -------------------->|
   |<-------------- 180 Ringing (early) -------------|
   |<-------------- 486 Busy Here -------------------|
   |--- ACK ---------------------------------------->|
   |                                                 |
   |            (Early Dialog Terminated)            |
```

### Behavior

- UAS sends 3xx-6xx instead of 200 OK
- Early dialog is terminated
- UAC sends ACK (part of transaction)

---

## Related Sections

- [Initiating Session](07-initiating-session.md) - INVITE
- [Dialogs](06-dialogs.md) - Dialog lifecycle
- [Modifying Session](08-modifying-session.md) - re-INVITE
- [Transactions](11-transactions.md) - Non-INVITE transaction
