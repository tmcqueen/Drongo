# Commit Guidelines

## Commit Message Format

Use conventional commit format with scope:

```
<type>(<scope>): <subject>

<body (optional)>

<footer (optional)>
```

### Types

- `feat` — New feature
- `fix` — Bug fix
- `docs` — Documentation only
- `test` — Test additions/changes
- `refactor` — Code change that neither fixes nor adds feature
- `perf` — Performance improvement
- `chore` — Build, dependency, tooling changes

### Scopes

Use the module/domain being changed:

- `core` — Drongo.Core changes
- `transport` — Transport layer
- `dialogs` — Dialog state management
- `media` — Media session handling
- `host` — Main application host
- `api` — Public API changes
- `config` — Configuration

### Examples

```bash
git commit -m "feat(dialogs): add dialog state machine transitions"
git commit -m "fix(transport): handle malformed UDP packets gracefully"
git commit -m "docs(api): document new IMediaSession interface"
git commit -m "test(dialogs): add tests for INVITE transaction timeout"
git commit -m "perf(core): optimize memory allocation in SIP parser"
```

---

## Phase Completion: Add a Haiku

When completing a phase, add a Haiku to the commit message to celebrate the milestone.

A Haiku is a three-line Japanese form of poetry with a **5-7-5 syllable structure**. It captures a moment or feeling related to the work completed.

### Phase 1 Example: Foundation

```bash
git commit -m "Complete Phase 1: Foundation

- Solution structure created
- Configuration and logging implemented
- Core abstractions defined

Haiku:
Foundations laid deep
Config flows like a river
Plugins wake from sleep"
```

### Phase 2 Example: Plugin Lifecycle

```bash
git commit -m "Complete Phase 2: Plugin Lifecycle

- PluginHost manages load/unload/reload
- ToolRegistry with thread-safety
- Native plugin loader with AssemblyLoadContext

Haiku:
Plugins dance and spin
Tools registered in the night
Code awakens now"
```

### Guidelines for Haikus

- Keep it work-related but have fun with it
- Strict 5 syllables / 7 syllables / 5 syllables
- Add after the main commit message body
- Celebrate the milestone!

### Syllable Counting Tips

Count carefully:

- "Foundations" = foun-DA-tions = 3 syllables
- "deeply" = DEEP-ly = 2 syllables
- "Config flows like a river" = CON-fig FLOWS like A RI-ver = 2+1+1+3 = 7 ✓

Use an online syllable counter when in doubt!

---

## Best Practices

- **One logical change per commit** — Don't mix feature work with refactoring
- **Write for future readers** — Explain *why*, not just *what*
- **Reference issues** — "Closes #123" in the footer if applicable
- **Keep messages concise** — Subject line < 70 characters
- **Use imperative mood** — "add" not "added", "fix" not "fixed"
