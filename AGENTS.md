# AGENTS.md

**Executable Paths**
- The beads CLI is /home/timm/.local/bin/bd

**Useful Skills**
- test-driven-development
- csharp-patterns
- context7

**Non-Negotiable Instructions**
- Always use the test-driven-development skill
- Always work in a worktree

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
