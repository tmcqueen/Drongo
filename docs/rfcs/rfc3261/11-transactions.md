# RFC 3261 - Transactions

## 17. Transactions

The **Transaction Layer** is a critical component that provides:
- Reliability for unreliable transports (UDP)
- Retransmission handling
- Response matching to requests
- Timeout handling

### Transaction Types

| Type | Description |
|------|-------------|
| **INVITE Transaction** | 3-way handshake (INVITE → 1xx/2xx → ACK) |
| **Non-INVITE Transaction** | 2-way handshake (request → response) |

---

## 17.1 Client Transaction

The client transaction is the transaction created by the Transaction User (TU) to send a request.

### INVITE Client Transaction

#### State Machine

```
                    +-----------+
                    |           |
        (+)-------->|  Calling  |
        |           |           |
        |           +-----------+
        |                 |
        |                 | 1xx received
        |                 v
        |           +-----------+
        |           |           |
        |   (+)<----| Proceeding|
        |   |       |           |
        |   |       +-----------+
        |   |             |
        |   |             | 300-699 received
        |   |             v
        |   |       +-----------+
        |   |       |           |
        |   +-------| Completed |
        |           |           |
        |           +-----------+
        |                 |
        |                 | Timer D fires
        |                 v
        |           +-----------+
        |           |           |
        +---------->| Terminated|
                    |           |
                    +-----------+

(+) = Transition from this state is same event
```

#### States

| State | Description |
|-------|-------------|
| **Calling** | Initial state - INVITE sent, waiting for response |
| **Proceeding** | Provisional response (1xx) received |
| **Completed** | Final response (300-699) received |
| **Terminated** | Transaction complete |

#### Events

| Event | Current State | Action |
|-------|---------------|--------|
| TU initiates INVITE | - | Enter "Calling", send INVITE, start Timer A, start Timer B |
| Timer A fires | Calling | Retransmit INVITE, reset Timer A to 2*T1 |
| Timer B fires | Calling | Inform TU of timeout, transition to Terminated |
| Transport error | Calling | Inform TU, transition to Terminated |
| 1xx received | Calling/Proceeding | Transition to Proceeding, pass to TU, stop retransmissions |
| 2xx received | Calling/Proceeding | Pass to TU, transition to Terminated |
| 300-699 received | Calling/Proceeding | Pass to TU, transition to Completed, send ACK, start Timer D |

#### Timer D

- **Purpose**: Wait for retransmissions of final response
- **Duration**: >=32s for UDP, 0s for TCP

---

### Non-INVITE Client Transaction

#### State Machine

```
                +-----------+
                |           |
    (+)-------->|  Trying   |
    |           |           |
    |           +-----------+
    |                 |
    |                 | 1xx received
    |                 v
    |           +-----------+
    |           |           |
    |   (+)<----| Proceeding|
    |   |       |           |
    |   |       +-----------+
    |   |             |
    |   |             | Any 200-699
    |   |             v
    |   |       +-----------+
    |   |       |           |
    |   +------>| Completed |
    |           |           |
    |           +-----------+
    |                 |
    |                 | Timer K fires
    |                 v
    |           +-----------+
    |           |           |
    +---------->| Terminated|
                |           |
                +-----------+
```

#### States

| State | Description |
|-------|-------------|
| **Trying** | Initial state - request sent |
| **Proceeding** | Provisional response (1xx) received |
| **Completed** | Final response (200-699) received |
| **Terminated** | Transaction complete |

#### Events

| Event | Current State | Action |
|-------|---------------|--------|
| TU initiates request | - | Enter "Trying", send request, start Timer F |
| Timer E fires | Trying | Retransmit request, reset Timer E to MIN(2*T1, T2) |
| Timer F fires | Trying | Inform TU of timeout, transition to Terminated |
| Transport error | Trying | Inform TU, transition to Terminated |
| 1xx received | Trying/Proceeding | Pass to TU, transition to Proceeding, Timer E becomes T2 |
| 200-699 received | Trying/Proceeding | Pass to TU, transition to Completed, start Timer K |
| Timer E fires | Proceeding | Retransmit request |
| Timer K fires | Completed | Transition to Terminated |

---

## 17.2 Server Transaction

### INVITE Server Transaction

#### State Machine

```
            +-----------+       ACK received       +-----------+
            |           | <-----------------------> |           |
    (+)<----|Proceeding|                           | Confirmed |
    |       |           |                           |           |
    |       +-----------+                           +-----------+
    |             |                                     |
    |             | 300-699 sent                        | Timer I fires
    |             v                                     v
    |       +-----------+                       +-----------+
    |       |           | --------------------> |           |
    +-----> | Completed |      Timer H fires     | Terminated|
            |           |                       |           |
            +-----------+                       +-----------+

(+) = Transition from this state is same event
```

#### States

| State | Description |
|-------|-------------|
| **Proceeding** | Initial - request received, awaiting TU response |
| **Completed** | Final non-2xx response sent, waiting for ACK |
| **Confirmed** | ACK received, absorbing late ACKs |
| **Terminated** | Transaction complete |

#### Events

| Event | Current State | Action |
|-------|---------------|--------|
| Request received | - | Enter "Proceeding", send 100 (Trying), pass to TU |
| TU sends 1xx | Proceeding | Forward response |
| Retransmit received | Proceeding | Retransmit last provisional |
| TU sends 2xx | Proceeding | Forward response, transition to Terminated |
| TU sends 300-699 | Proceeding | Forward response, transition to Completed, start Timer G |
| Timer G fires | Completed | Retransmit response, reset Timer G to MIN(2*T1, T2) |
| Retransmit received | Completed | Retransmit final response |
| ACK received | Completed | Transition to Confirmed, stop Timer G |
| Timer H fires | Completed | Transition to Terminated, inform TU |
| Timer I fires | Confirmed | Transition to Terminated |

---

### Non-INVITE Server Transaction

#### State Machine

```
            +-----------+       1xx sent             +-----------+
            |           | <------------------------- |           |
    (+)<----|  Trying   |                           | Proceeding|
    |       |           |                           |           |
    |       +-----------+                           +-----------+
    |             |                                     |
    |             | 200-699 sent                        | Timer J fires
    |             v                                     v
    |       +-----------+                       +-----------+
    |       |           | --------------------> |           |
    +-----> | Completed |      Timer J fires     | Terminated|
            |           |                       |           |
            +-----------+                       +-----------+

(+) = Transition from this state is same event
```

#### States

| State | Description |
|-------|-------------|
| **Trying** | Initial - request received |
| **Proceeding** | Provisional response sent |
| **Completed** | Final response sent |
| **Terminated** | Transaction complete |

#### Events

| Event | Current State | Action |
|-------|---------------|--------|
| Request received | - | Enter "Trying", pass to TU |
| Retransmit received | Trying | Discard |
| TU sends 1xx | Trying/Proceeding | Forward response, transition to Proceeding |
| TU sends 200-699 | Trying/Proceeding | Forward response, transition to Completed, start Timer J |
| Retransmit received | Proceeding | Retransmit provisional |
| Retransmit received | Completed | Retransmit final |
| Timer J fires | Completed | Transition to Terminated |

---

## 17.3 Matching Responses to Transactions

### Client Transaction Matching

Match response to transaction by:
1. **Branch** parameter in Via (must start with `z9hG4bK`)
2. **Sent-by** address matches
3. **CSeq method** matches (except ACK matches INVITE)

### Server Transaction Matching

Match request to transaction by:
1. **Branch** parameter (with magic cookie)
2. **Sent-by** address
3. **Method** (ACK is matched to INVITE transaction)

---

## Timer Values Summary

| Timer | Default | Used For | Purpose |
|-------|---------|----------|---------|
| **T1** | 500ms | All | Round-trip time estimate |
| **T2** | 4s | Non-INVITE | Max retransmit interval |
| **Timer A** | T1 | INVITE client | INVITE retransmit |
| **Timer B** | 64*T1 | INVITE client | INVITE timeout |
| **Timer D** | 32s (UDP), 0 (TCP) | INVITE client | Wait for response retransmits |
| **Timer E** | T1 | Non-INVITE client | Request retransmit |
| **Timer F** | 64*T1 | Non-INVITE client | Request timeout |
| **Timer G** | T1 | INVITE server | Response retransmit |
| **Timer H** | 64*T1 | INVITE server | Wait for ACK |
| **Timer I** | T4 (5s UDP), 0 TCP | INVITE server | Wait for ACK retransmits |
| **Timer J** | 64*T1 (UDP), 0 TCP | Non-INVITE server | Wait for request retransmits |
| **Timer K** | T4 (5s UDP), 0 TCP | Non-INVITE client | Wait for response retransmits |

---

## Related Sections

- [Initiating Session](07-initiating-session.md) - INVITE transaction
- [Messages](03-messages.md) - Via header
- [Transport](12-transport.md) - Transport layer
- [Timer Values](16-timer-values.md) - Appendix A details
