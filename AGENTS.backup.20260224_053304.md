# AGENTS.md

**Navigation:** See `docs/index.yaml`

**Quick commands:**
```bash
dotnet build && dotnet test    # Verify everything works
bd ready                       # Find a task to claim
bd update <id> --status=in_progress
bd close <id> --reason=Description
bd sync && git push            # End of session (MANDATORY)
```

**Critical:** Always follow [docs/SESSION_COMPLETION.md](docs/SESSION_COMPLETION.md) before ending your session.

---

See `docs/index.yaml` for full navigation, guides, architecture, and use cases.
