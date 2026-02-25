# Landing the Plane: Session Completion Workflow

**When ending a work session**, you MUST complete ALL steps below. Work is NOT complete until `git push` succeeds.

---

## MANDATORY WORKFLOW

### 1. File Issues for Remaining Work

Create issues for anything that needs follow-up. Do not leave work implicit.

```bash
bd create --title="Fix remaining edge case in SipParser" --type=bug --priority=2
```

---

### 2. Run Quality Gates

If code changed, verify everything passes:

```bash
dotnet build                       # Compile + run analyzers
dotnet test                        # Run all tests
dotnet format --verify-no-changes  # Check formatting
```

All must pass before proceeding.

---

### 3. Update Issue Status

Close finished work, update in-progress items:

```bash
bd close <id> --reason="Completed: Added dialog state machine transitions"
bd update <id> --status=in_progress  # If pausing work
```

---

### 4. PUSH TO REMOTE (MANDATORY)

This is non-negotiable. Work stranded locally is lost work.

```bash
git pull --rebase        # Get latest remote changes
bd sync                  # Commit beads changes
git push                 # Push everything to remote
git status               # MUST show "up to date with origin"
```

---

### 5. Clean Up

Remove clutter:

```bash
git stash clear          # Clear any stashes
git remote prune origin  # Clean remote tracking branches
```

---

### 6. Verify

Double-check that ALL changes are pushed:

```bash
git status               # Should show "working tree clean"
git log -1               # Last commit should be yours
git push --dry-run       # Verify nothing pending
```

---

### 7. Hand Off

Document what was accomplished and what's next:

```
Session complete.

Accomplished:
- Implemented dialog state machine transitions
- Added 8 new tests covering edge cases
- Updated documentation for dialog lifecycle

Next steps:
- Issue #47: Handle timeout transitions
- Issue #48: Add tracing for state changes
```

---

## CRITICAL RULES

```
🚨 Work is NOT complete until `git push` succeeds 🚨
```

- **NEVER** stop before pushing — that leaves work stranded locally
- **NEVER** say "ready to push when you are" — YOU must push
- **If push fails**, resolve and retry until it succeeds
- **All changes must be committed and pushed** — `git status` must show clean

---

## Checklist

Print this out if needed:

- [ ] Quality gates pass (`dotnet build && dotnet test`)
- [ ] Issues filed for remaining work (`bd create`)
- [ ] Finished issues closed (`bd close <id>`)
- [ ] In-progress issues updated (`bd update <id> --status=in_progress`)
- [ ] `git pull --rebase` completed without conflicts
- [ ] `bd sync` run to commit beads changes
- [ ] `git push` succeeded (not dry-run)
- [ ] `git status` shows "working tree clean"
- [ ] Hand-off notes written

---

## Why This Matters

This workflow prevents:
- Lost work due to local-only changes
- Forgotten issues languishing
- Unclear project state for next session
- Merge conflicts from stale branches
- Beads inconsistencies (issues in git but not tracked)

**Discipline in process = clarity in execution.**
