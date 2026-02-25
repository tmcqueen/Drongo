# Additional Resources

Quick reference links to architecture, design, and technical documentation.

---

## Project Documentation

| Document | Purpose |
|----------|---------|
| [overview.md](overview.md) | High-level project overview and architecture |
| [api-surface.md](api-surface.md) | Public API reference and contracts |
| [concurrency.md](concurrency.md) | Concurrency patterns and thread-safety |
| [distributed-architecture.md](distributed-architecture.md) | Multi-instance deployment patterns |

---

## Development Guides

| Guide | Purpose |
|-------|---------|
| [BUILDING_AND_TESTING.md](BUILDING_AND_TESTING.md) | Build commands and testing |
| [CODE_STYLE.md](CODE_STYLE.md) | Naming, structure, async patterns |
| [COMMIT_GUIDELINES.md](COMMIT_GUIDELINES.md) | Commit message format and haikus |
| [SESSION_COMPLETION.md](SESSION_COMPLETION.md) | End-of-session workflow |
| [BEADS_WORKFLOW.md](BEADS_WORKFLOW.md) | Issue tracking and workflow |
| [AGENT_INSTRUCTIONS.md](AGENT_INSTRUCTIONS.md) | Documentation and agent patterns |

---

## Architecture Documentation

Detailed architecture for each major component:

```
docs/internals/
├── Drongo.Core/        # Core SIP engine
├── Drongo.Media/       # Media session handling
└── Drongo/             # Main application host
```

Each directory contains:
- `Index.md` — Contents and navigation
- `Overview.md` — Design and architecture
- Component-specific docs (Dialogs, Transport, etc.)

---

## RFC & Protocol Reference

| Reference | Purpose |
|-----------|---------|
| [RFC_REFERENCE.md](RFC_REFERENCE.md) | Pointer to RFC 3261 documentation |
| `docs/rfcs/rfc3261/` | Detailed SIP protocol reference |
| [RFC 3261 Full Text](https://tools.ietf.org/html/rfc3261) | Official IETF specification |

---

## Quick Links by Use Case

### "How do I...?"

- **...build the project?** → [BUILDING_AND_TESTING.md](BUILDING_AND_TESTING.md)
- **...run tests?** → [BUILDING_AND_TESTING.md](BUILDING_AND_TESTING.md#test-commands)
- **...format my code?** → [CODE_STYLE.md](CODE_STYLE.md)
- **...write a commit?** → [COMMIT_GUIDELINES.md](COMMIT_GUIDELINES.md)
- **...end my session?** → [SESSION_COMPLETION.md](SESSION_COMPLETION.md)
- **...claim a task?** → [BEADS_WORKFLOW.md](BEADS_WORKFLOW.md)
- **...write documentation?** → [AGENT_INSTRUCTIONS.md](AGENT_INSTRUCTIONS.md)

### "What is...?"

- **...the overall architecture?** → [overview.md](overview.md)
- **...the public API?** → [api-surface.md](api-surface.md)
- **...how threading works?** → [concurrency.md](concurrency.md)
- **...the distributed design?** → [distributed-architecture.md](distributed-architecture.md)
- **...how SIP dialogs work?** → [RFC_REFERENCE.md](RFC_REFERENCE.md)

### "I need to implement..."

- **...a SIP feature** → Check [RFC_REFERENCE.md](RFC_REFERENCE.md), then look in `docs/internals/Drongo.Core/`
- **...a media feature** → Look in `docs/internals/Drongo.Media/`
- **...a new tool** → Read `docs/api-surface.md`
- **...async code** → Consult [CODE_STYLE.md](CODE_STYLE.md#async-patterns)

---

## External References

### Standards & Specifications

- [RFC 3261: Session Initiation Protocol](https://tools.ietf.org/html/rfc3261)
- [SDP: Session Description Protocol](https://tools.ietf.org/html/rfc4566)
- [DTLS: Datagram Transport Layer Security](https://tools.ietf.org/html/rfc6347)

### .NET & Libraries

- [NAudio Documentation](https://github.com/naudio/NAudio)
- [Microsoft.Extensions.Hosting](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting)
- [Microsoft.Extensions.DependencyInjection](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection)
- [xUnit.net](https://xunit.net/)
- [NSubstitute](https://nsubstitute.github.io/)
- [Shouldly](https://shouldly.io/)

---

## Directory Map

```
.
├── src/                          # Source code
│   ├── Drongo/                   # Main application host
│   ├── Drongo.Core/              # Core SIP engine
│   └── Drongo.Media/             # Media session handling
├── docs/                         # Documentation root
│   ├── overview.md               # Architecture overview
│   ├── api-surface.md            # Public API
│   ├── concurrency.md            # Threading & concurrency
│   ├── distributed-architecture.md
│   ├── internals/                # Implementation details
│   │   ├── Drongo.Core/
│   │   ├── Drongo.Media/
│   │   └── Drongo/
│   └── rfcs/rfc3261/             # RFC documentation
├── tests/                        # Test projects
│   └── Drongo.Core.Tests/
├── examples/                     # Example projects
├── .beads/                       # Issue tracking (git-tracked)
└── AGENTS.md                     # This file's navigation hub
```

---

## How to Navigate

1. **Start here**: Read [overview.md](overview.md) for context
2. **Need to work**: Check [BEADS_WORKFLOW.md](BEADS_WORKFLOW.md) to claim a task
3. **Implementing code**: Consult [CODE_STYLE.md](CODE_STYLE.md)
4. **Stuck on SIP?**: Check [RFC_REFERENCE.md](RFC_REFERENCE.md)
5. **Writing docs**: Use [AGENT_INSTRUCTIONS.md](AGENT_INSTRUCTIONS.md)
6. **Ending session**: Follow [SESSION_COMPLETION.md](SESSION_COMPLETION.md)

---

## Document Maintenance

Each guide is maintained independently. If you find:

- **Outdated info** → File an issue and update the doc
- **Missing guidance** → Add a section and document it
- **Broken links** → Fix the link and add to a resource
- **Incomplete reference** → Expand the resource

Keep the navigation working by updating relevant index/link sections when adding new docs.
