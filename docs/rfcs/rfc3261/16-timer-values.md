# RFC 3261 - Timer Values (Appendix A)

## Timer Summary

This appendix defines the default timer values used in SIP transactions.

---

## Base Timers

| Timer | Default Value | Description |
|-------|---------------|-------------|
| **T1** | 500 ms | Round-Trip Time (RTT) estimate |
| **T2** | 4 seconds | Maximum retransmission interval for non-INVITE and INVITE responses |
| **T4** | 5 seconds | Maximum time a message can stay in the network |

---

## INVITE Transaction Timers

### Client Transaction (UAC)

| Timer | Default | Description |
|-------|---------|-------------|
| **Timer A** | Initially T1 (500ms) | INVITE request retransmission interval |
| **Timer B** | 64 × T1 (32 seconds) | INVITE transaction timeout |
| **Timer D** | ≥ 32 seconds (UDP), 0 (TCP) | Wait time for INVITE response retransmissions |

**Behavior:**
- Timer A doubles after each retransmission (T1 → 2T1 → 4T1 → ...)
- Timer B fires: transaction has timed out
- Timer D fires: can discard transaction state

### Server Transaction (UAS)

| Timer | Default | Description |
|-------|---------|-------------|
| **Timer G** | T1 (500ms) | INVITE response retransmission interval |
| **Timer H** | 64 × T1 (32 seconds) | Wait time for ACK receipt |
| **Timer I** | T4 (5 seconds UDP), 0 (TCP) | Wait time for ACK retransmissions |

**Behavior:**
- Timer G doubles after each retransmission (T1 → 2T1 → 4T1 → ...)
- Timer H fires: ACK never received, terminate transaction
- Timer I fires: can discard transaction state

---

## Non-INVITE Transaction Timers

### Client Transaction (UAC)

| Timer | Default | Description |
|-------|---------|-------------|
| **Timer E** | Initially T1 (500ms) | Request retransmission interval |
| **Timer F** | 64 × T1 (32 seconds) | Non-INVITE transaction timeout |
| **Timer K** | T4 (5 seconds UDP), 0 (TCP) | Wait time for response retransmissions |

**Behavior:**
- Timer E doubles to max T2 (4 seconds)
- Timer F fires: transaction has timed out
- Timer K fires: can discard transaction state

### Server Transaction (UAS)

| Timer | Default | Description |
|-------|---------|-------------|
| **Timer J** | 64 × T1 (32 seconds UDP), 0 (TCP) | Wait time for request retransmissions |

**Behavior:**
- Timer J fires: can discard transaction state

---

## Timer Calculation Summary

```
T1 = 500 ms
T2 = 4000 ms (4 seconds)
T4 = 5000 ms (5 seconds)

INVITE Client:
  Timer A = T1 (then doubles: 2T1, 4T1, ...)
  Timer B = 64 * T1 = 32 seconds
  Timer D = 32 seconds (UDP), 0 (TCP)

INVITE Server:
  Timer G = T1 (then doubles: 2T1, 4T1, ...)
  Timer H = 64 * T1 = 32 seconds  
  Timer I = 5 seconds (UDP), 0 (TCP)

Non-INVITE Client:
  Timer E = T1 (max T2)
  Timer F = 64 * T1 = 32 seconds
  Timer K = 5 seconds (UDP), 0 (TCP)

Non-INVITE Server:
  Timer J = 32 seconds (UDP), 0 (TCP)
```

---

## Timer Configuration

### Adjusting T1

- Lower T1: Faster timeout, more network traffic
- Higher T1: Slower timeout, less network traffic
- T1 should be approximately the RTT

### Production Considerations

- Timer B/F (transaction timeout) may need adjustment for slow networks
- Timer C (proxy INVITE timeout) defaults to 3 minutes
- Consider bandwidth, latency, and reliability

---

## Related Sections

- [Transactions](11-transactions.md) - State machine details
- [Transport](12-transport.md) - Transport considerations
