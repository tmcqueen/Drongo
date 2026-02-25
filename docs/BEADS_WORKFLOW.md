# Beads Workflow Integration

This project uses [beads_viewer](https://github.com/Dicklesworthstone/beads_viewer) for issue tracking. Issues are stored in `.beads/` and tracked in git.

---

## Essential Commands

### CLI Commands (Use These for Agents)

```bash
bd ready              # Show issues ready to work (no blockers)
bd list --status=open # All open issues
bd show <id>          # Full issue details with dependencies
bd create --title="..." --type=task --priority=2
bd update <id> --status=in_progress
bd close <id> --reason="Completed"
bd close <id1> <id2>  # Close multiple issues at once
bd sync               # Commit and push changes
```

### TUI (Avoid in Automated Sessions)

```bash
bv                    # Launch interactive TUI
```

---

## Workflow Pattern

### 1. Start Session

Find actionable work:

```bash
bd ready              # Shows only issues with no blockers
```

### 2. Claim Work

Mark the issue as in progress:

```bash
bd update <id> --status=in_progress
```

### 3. Work

Implement the task, run tests, update code.

### 4. Complete

Close the issue with a reason:

```bash
bd close <id> --reason="Completed: Added dialog state machine transitions"
```

### 5. Sync at Session End

Always run before ending:

```bash
bd sync               # Commits beads changes to git
git push              # Push to remote
```

---

## Key Concepts

### Dependencies

Issues can block other issues. `bd ready` shows only unblocked work.

```bash
bd dep add <issue> <depends-on>    # Add dependency
bd show <id>                       # See blocking issues
```

### Priority Levels

Use numbers, not words:

- `P0` or `0` — Critical (blocker)
- `P1` or `1` — High (important)
- `P2` or `2` — Medium (normal)
- `P3` or `3` — Low (nice to have)
- `P4` or `4` — Backlog (future consideration)

```bash
bd create --title="Parse SIP headers" --priority=1
bd update <id> --priority=2
```

### Issue Types

```bash
bd create --type=task      # General task
bd create --type=bug       # Bug fix
bd create --type=feature   # New feature
bd create --type=epic      # Large feature group
bd create --type=question  # Question/discussion
bd create --type=docs      # Documentation
```

---

## Session Protocol

**Before ending any session, run this checklist:**

```bash
git status              # Check what changed
git add <files>         # Stage code changes
bd sync                 # Commit beads changes AND code
git push                # Push to remote
git status              # Verify clean: "working tree clean"
```

If `bd sync` commits code changes, you're done. If not:

```bash
git commit -m "feat(dialogs): add state machine"
bd sync                 # Commit any new beads changes
git push
```

---

## Best Practices

- **Check `bd ready` at session start** — Find available work first
- **Update status as you work** — in_progress → closed
- **Create issues when you discover tasks** — Don't leave implicit work
- **Use descriptive titles** — "Add dialog state machine" not "Add stuff"
- **Set priority and type** — Helps next session prioritize
- **`bd sync` before pushing** — Keeps beads and git in sync
- **Document your reasoning** — Close reason explains what/why

### Example Session

```bash
# Start
bd ready
# Shows: "BD-1: Implement dialog state machine (no blockers)"

# Claim
bd update BD-1 --status=in_progress

# Work
# ... edit code, run tests ...

# Complete
bd close BD-1 --reason="Completed: Added INVITE/BYE state transitions with tests"

# Discover new work
bd create --title="Add CANCEL transaction support" --type=feature --priority=1

# End session
bd sync
git push
```

---

## Critical Rules

1. **Always use beads for multi-session work** — TodoWrite tasks are lost on compaction
2. **Update status when starting work** — Mark tasks as `in_progress`
3. **Close with completion reason** — Document what was accomplished
4. **Sync before ending session** — `bd sync` persists to git
5. **Check dependencies** — Use `bd show <id>` to see blockers before claiming

---

## Troubleshooting

### Issue Not Appearing in Ready

```bash
bd show <id>                # Check if it has blockers
bd dep tree <id>            # View full dependency tree
```

Remove blocker or add a different dependency:

```bash
bd dep remove <blocker-id>
bd dep add <different-id>
```

### Sync Failed

```bash
git status              # Check for conflicts
git pull --rebase       # Resolve conflicts
bd sync                 # Try again
```

### Lost Local Changes

```bash
git reflog              # Find your commits
git reset --hard <commit>  # Recover if needed
```
# Beads Workflow Context

> **Context Recovery**: Run `bd prime` after compaction, clear, or new session
> Hooks auto-call this in Claude Code when .beads/ detected

# 🚨 SESSION CLOSE PROTOCOL 🚨

**CRITICAL**: Before saying "done" or "complete", you MUST run this checklist:

```
[ ] 1. git status              (check what changed)
[ ] 2. git add <files>         (stage code changes)
[ ] 3. git commit -m "..."     (commit code)
[ ] 4. git push                (push to remote)
```

**NEVER skip this.** Work is not done until pushed.

## Core Rules
- **Default**: Use beads for ALL task tracking (`bd create`, `bd ready`, `bd close`)
- **Prohibited**: Do NOT use TodoWrite, TaskCreate, or markdown files for task tracking
- **Workflow**: Create beads issue BEFORE writing code, mark in_progress when starting
- Persistence you don't need beats lost context
- Git workflow: beads auto-commit to Dolt, run `git push` at session end
- Session management: check `bd ready` for available work

## Essential Commands

### Finding Work
- `bd ready` - Show issues ready to work (no blockers)
- `bd list --status=open` - All open issues
- `bd list --status=in_progress` - Your active work
- `bd show <id>` - Detailed issue view with dependencies

### Creating & Updating
- `bd create --title="Summary of this issue" --description="Why this issue exists and what needs to be done" --type=task|bug|feature --priority=2` - New issue
  - Priority: 0-4 or P0-P4 (0=critical, 2=medium, 4=backlog). NOT "high"/"medium"/"low"
- `bd update <id> --status=in_progress` - Claim work
- `bd update <id> --assignee=username` - Assign to someone
- `bd update <id> --title/--description/--notes/--design` - Update fields inline
- `bd close <id>` - Mark complete
- `bd close <id1> <id2> ...` - Close multiple issues at once (more efficient)
- `bd close <id> --reason="explanation"` - Close with reason
- **Tip**: When creating multiple issues/tasks/epics, use parallel subagents for efficiency
- **WARNING**: Do NOT use `bd edit` - it opens $EDITOR (vim/nano) which blocks agents

### Dependencies & Blocking
- `bd dep add <issue> <depends-on>` - Add dependency (issue depends on depends-on)
- `bd blocked` - Show all blocked issues
- `bd show <id>` - See what's blocking/blocked by this issue

### Sync & Collaboration
- `bd dolt push` - Push beads to Dolt remote
- `bd dolt pull` - Pull beads from Dolt remote
- `bd search <query>` - Search issues by keyword

### Project Health
- `bd stats` - Project statistics (open/closed/blocked counts)
- `bd doctor` - Check for issues (sync problems, missing hooks)

## Common Workflows

**Starting work:**
```bash
bd ready           # Find available work
bd show <id>       # Review issue details
bd update <id> --status=in_progress  # Claim it
```

**Completing work:**
```bash
bd close <id1> <id2> ...    # Close all completed issues at once
git add . && git commit -m "..."  # Commit code changes
git push                    # Push to remote
```

**Creating dependent work:**
```bash
# Run bd create commands in parallel (use subagents for many items)
bd create --title="Implement feature X" --description="Why this issue exists and what needs to be done" --type=feature
bd create --title="Write tests for X" --description="Why this issue exists and what needs to be done" --type=task
bd dep add beads-yyy beads-xxx  # Tests depend on Feature (Feature blocks tests)
```
