# Agent Instructions

Instructions for AI agents working on the Drongo project.

---

## Documentation Generation

At the end of every development phase, create documentation in the `docs/internals/<PROJECT_NAME>/` directory.

### What to Document

- **Overview document** — Design decisions, dependencies, critical information
- **Namespace architecture** — Detailed docs for each namespace
- **Interaction diagrams** — How components connect

### Files to Create

```
docs/internals/
├── Drongo.Core/
│   ├── Index.md              # Contents and deep links
│   ├── Overview.md           # Architecture, design decisions
│   ├── Dialogs.md            # Dialog state machine details
│   ├── Transport.md          # Transport layer architecture
│   └── Transactions.md       # Transaction handling
├── Drongo.Media/
│   ├── Index.md
│   └── Overview.md
└── Drongo/
    ├── Index.md
    └── Overview.md
```

### Index.md Template

```markdown
# Drongo.Core Architecture

## Contents

- [Overview](Overview.md) — High-level design and concepts
- [Dialogs](Dialogs.md) — Dialog state machine implementation
- [Transport](Transport.md) — Transport layer architecture
- [Transactions](Transactions.md) — Transaction handling and state

## Key Concepts

- Immutable SIP request/response records
- State machine-driven dialog lifecycle
- Transport-agnostic core

## Critical Decisions

1. **Immutable Records** — DTOs use `record` for thread-safety
2. **DI Everything** — No static state, all dependencies injected
3. **RFC 3261 Compliance** — State machines follow RFC exactly
```

---

## Skills to Use

Use these skills for documentation and diagrams:

### writing-clearly-and-concisely

For all documentation writing:

```bash
# Instead of:
# "This component, which is responsible for managing the state of dialogs..."

# Write:
# "Manages dialog state machine transitions."
```

**Use for:**
- Overview documents
- Namespace documentation
- Inline explanations

### mermaid-diagrams

For architecture and flow diagrams:

```bash
# Dialog state machine diagram
# Message flow diagrams
# Component interaction diagrams
# Class hierarchy diagrams
```

**Use for:**
- State machines (graph diagrams)
- Sequence diagrams (message flows)
- Class diagrams (component relationships)

---

## Documentation Quality Checklist

- [ ] Overview explains **purpose** of the namespace
- [ ] Design decisions documented with **rationale**
- [ ] Dependencies clearly listed
- [ ] Code examples for complex patterns
- [ ] Diagrams for state machines and flows
- [ ] Critical classes/interfaces highlighted
- [ ] Cross-references to related namespaces
- [ ] External references (RFC sections, etc.)

---

## Phase Completion Documentation

When completing a phase:

1. **Create overview** — What was built this phase?
2. **Document architecture** — How do components interact?
3. **List critical decisions** — Why were choices made?
4. **Add examples** — Show usage patterns
5. **Link to RFC** — Reference relevant protocol sections

### Phase Completion Commit

Include phase documentation in your commit:

```bash
git commit -m "Complete Phase 2: Dialog Lifecycle

- Dialog creation and state management
- INVITE/BYE transaction handling
- Media session coordination

Docs:
- docs/internals/Drongo.Core/Dialogs.md added
- docs/internals/Drongo.Core/Transactions.md added
- State machine diagrams in Mermaid

Haiku:
Dialogs take shape
State machines dance together
Calls flow through the net"
```

---

## Standards

### Markdown

- Clear headings hierarchy (`#`, `##`, `###`)
- Code examples with language markers (```csharp, ```bash)
- Tables for comparison
- Bullet lists for multiple items
- Emphasis for **important**, *italics* for context

### Diagrams

- Use Mermaid for all diagrams (no external images)
- Label all states and transitions clearly
- Use meaningful colors/grouping
- Include a caption explaining the diagram

### Code Examples

- Must compile and run
- Include both happy path and error handling
- Use realistic names (not `foo`, `bar`)
- Comment complex sections

---

## What NOT to Document

- Implementation details that users don't need
- Deprecated patterns or old decisions
- One-off bug fixes
- Duplicate information (link instead)

---

## Keeping Docs Current

After completing work:

1. **Check existing docs** — Do they need updates?
2. **Update if changed** — Reflect new reality
3. **Link new docs** — Add to Index.md
4. **Verify links** — No broken references
5. **Commit together** — Code + docs in same commit

---

## Questions?

Consult these reference docs:

- [CODE_STYLE.md](CODE_STYLE.md) — Naming, structure, patterns
- [RFC_REFERENCE.md](RFC_REFERENCE.md) — Protocol definitions
- [docs/overview.md](overview.md) — High-level architecture
