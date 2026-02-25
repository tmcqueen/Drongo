# RFC Documentation Reference

**Source of Truth for SIP-related questions.**

---

## RFC 3261: SIP Protocol

The RFC 3261 documentation in `docs/rfcs/rfc3261/` contains detailed, searchable summaries of all SIP protocol concepts.

**Before implementing any SIP-related feature, consult these documents.**

### Key References

| Document | Purpose |
|----------|---------|
| `docs/rfcs/rfc3261/index.yaml` | Searchable index of all concepts |
| `docs/rfcs/rfc3261/06-dialogs.md` | Dialog creation and B2BUA patterns |
| `docs/rfcs/rfc3261/11-transactions.md` | Transaction state machines |
| `docs/rfcs/rfc3261/16-timer-values.md` | RFC timer defaults and values |

---

## How to Use RFC Reference

### 1. Search by Concept

Use `index.yaml` to find which section covers a topic:

```bash
# Looking for INVITE handling?
grep -i "invite" docs/rfcs/rfc3261/index.yaml
# Points to: 06-dialogs.md, 11-transactions.md
```

### 2. Consult Relevant Section

Read the detailed markdown for implementation guidance:

```bash
# Understanding dialog state machine?
cat docs/rfcs/rfc3261/06-dialogs.md
```

### 3. Reference in Code

When implementing SIP logic, cite the RFC section:

```csharp
// RFC 3261 Section 11.1: INVITE transaction state machine
public enum InviteTransactionState
{
    Calling,      // Waiting for response
    Proceeding,   // Received 1xx
    Completed,    // Received final response
}
```

---

## Common Queries

### "How do dialogs work?"

→ See `docs/rfcs/rfc3261/06-dialogs.md`

### "What are the timer values?"

→ See `docs/rfcs/rfc3261/16-timer-values.md`

### "How do transactions transition states?"

→ See `docs/rfcs/rfc3261/11-transactions.md`

### "How should I handle [SIP feature]?"

→ Check `docs/rfcs/rfc3261/index.yaml` for relevant section

---

## RFC Structure in This Project

```
docs/rfcs/rfc3261/
├── index.yaml              # Searchable concept index
├── 06-dialogs.md           # Dialog lifecycle
├── 11-transactions.md      # Transaction state machines
├── 16-timer-values.md      # Timer specifications
└── [other sections...]
```

Each document includes:
- Relevant RFC section number
- Protocol specification details
- Simplified explanations
- Implementation guidance
- Examples and diagrams

---

## Rules for Using RFC Reference

1. **Always check RFC first** before implementing SIP features
2. **Cite RFC section** in code comments
3. **Follow state machines exactly** — Don't improvise
4. **Use correct timer values** — Don't guess
5. **Document deviations** — If you deviate from RFC, explain why

---

## When RFC Doesn't Cover It

If you need SIP protocol information not in the RFC docs:

1. Consult [RFC 3261 Full Text](https://tools.ietf.org/html/rfc3261)
2. Document your findings in the appropriate `docs/rfcs/rfc3261/` file
3. Update `index.yaml` to include new concept
4. Submit as part of your commit

---

## Related References

- [CODE_STYLE.md](CODE_STYLE.md) — SIP-specific conventions
- [docs/overview.md](overview.md) — Architecture overview
- [docs/internals/Drongo.Core/](internals/Drongo.Core/) — Implementation details
