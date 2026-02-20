# AGENTS.md - Drongo Development Guide

## Project Overview

Drongo is a .NET-native SIP (Session Initiation Protocol) runtime / softswitch. It follows ASP.NET Core patterns for hosting and middleware, providing a deterministic, high-performance SIP stack over UDP (with TCP/TLS planned).

- **.NET 10+** / **C# 13+** with nullable reference types
- **NAudio** for media playback/recording
- **Microsoft.Extensions.*** for hosting, DI, logging, configuration

---

## Build Commands

```bash
dotnet restore                    # Restore dependencies
dotnet build                      # Build the solution
dotnet build --configuration Release
dotnet build src/Drongo.Core/Drongo.Core.csproj
dotnet run --project src/Drongo/Drongo.csproj
```

## Testing

_____________________________________
****UNDER NO CIRCUMSTANCES SHOULD A TASK CLOSE WITHOUT TESTS WRITTEN AND PASSING****
_____________________________________

Every task must include unit tests. Each file modified should trigger an update to the relevant unit test, and each session should end with all tests passing.

### Testing Stack

| Component | Technology |
|----------|------------|
| Framework | **xUnit v3** with Microsoft.Testing.Platform v2 |
| Mocking | **NSubstitute** |
| Assertions | **Shouldly** |
| Code Coverage | **Microsoft.Testing.Extensions.Coverage** with **coverlet.collector** |

### Test Commands

```bash
# Run all tests
dotnet test

# Run tests with code coverage
dotnet test /p:CollectCoverage=true /p:Threshold=0 /p:CoverletOutputFormat=cobertura /p:CoverletOutput=coverage.cobertura.xml

# Run a single test
dotnet test --filter "FullyQualifiedName~SipParserTests.ParseRequest_ValidInvite_ReturnsSuccess"

# Run tests in a specific project
dotnet test tests/Drongo.Core.Tests/Drongo.Core.Tests.csproj
```

### Testing Guidelines

- Use **xUnit v3** with `[Fact]` attribute
- Use **NSubstitute** for mocking dependencies
- Use **Shouldly** for assertions (fluent style)
- Name tests: `<MethodName>_<Scenario>_<ExpectedResult>`
- Use Arrange/Act/Assert structure
- Mock external dependencies (network, timers, time)

```csharp
using NSubstitute;
using Shouldly;

[Fact]
public void ParseRequest_ValidInvite_ReturnsSuccess()
{
    // Arrange
    var parser = new SipParser();
    var data = "INVITE sip:bob@biloxi.com SIP/2.0\r\n" +
               "Via: SIP/2.0/UDP pc33.atlanta.com;branch=z9hG4bK776asdhds\r\n" +
               "To: Bob <sip:bob@biloxi.com>\r\n" +
               "From: Alice <sip:alice@atlanta.com>;tag=1928301774\r\n" +
               "Call-ID: test\r\n" +
               "CSeq: 1 INVITE\r\n" +
               "Content-Length: 0\r\n" +
               "\r\n";

    // Act
    var result = parser.ParseRequest(new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(data)));

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Request.ShouldNotBeNull();
    result.Request!.Method.ShouldBe(SipMethod.Invite);
}
```

## Linting & Code Quality

```bash
dotnet build                       # Run analyzers
dotnet tool install --global dotnet-format
dotnet format                      # Format code
dotnet format --verify-no-changes --verbosity diagnostic
```

---

## Code Style Guidelines

### General Principles
- **No static state** - Everything must be dependency-injected
- **Deterministic transactions** - SIP state machines must be predictable
- **Transport isolation** - Core SIP engine remains transport-neutral
- **Request-scoped context** - Context objects are short-lived per request

### Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Interfaces | `I` prefix | `ISipChannel`, `IDialog` |
| Classes | PascalCase | `SipRequest`, `InviteContext` |
| Methods | PascalCase | `SendRequestAsync`, `ResolveAsync` |
| Properties | PascalCase | `CallId`, `RequestUri` |
| Private fields | `_camelCase` | `_registrar`, `_transport` |
| Constants | PascalCase | `DefaultPort = 5060` |
| Enums | PascalCase | `DialogState`, `SipMethod` |

### Directory Structure

All source files go in `src/`, all project documentation goes in `docs/`, all tests go in `tests/`, and all examples go in `examples/`.

```
src/                    # Source code
  Drongo/              # Main application host
  Drongo.Core/         # Core SIP engine (Dialogs, Transport, Messaging)
  Drongo.Media/        # NAudio media sessions
docs/                  # Project documentation
tests/                 # Test projects
  Drongo.Core.Tests/
examples/              # Example projects and samples
```

### File Organization

### Imports
- Use **global usings** in `GlobalUsings.cs`
- Order: `System.*`, third-party, project-specific

```csharp
global using System.Net;
global using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Drongo.Core.Dialogs;
```

### Types & Nullability
- Enable `<Nullable>enable</Nullable>`
- Use `record` for immutable DTOs (`SipRequest`, `ContactBinding`)
- Use `class` for entities with mutable state (`Dialog`)
- Use `struct` for performance-critical value types

```csharp
public sealed record SipRequest(
    string Method,
    SipUri RequestUri,
    IReadOnlyDictionary<string, string> Headers,
    ReadOnlyMemory<byte> Body);

public sealed class Dialog : IDialog
{
    public string CallId { get; private set; }
    public IMediaSession? Media { get; set; }  // Nullable
}
```

### Error Handling
- Use custom exceptions for domain-specific errors
- Throw `ArgumentNullException` for null checks on required parameters
- Use `InvalidOperationException` for state violations
- Prefer result types for expected failures in parsing/validation

```csharp
public class SipParseException : Exception
{
    public int Position { get; }
    public SipParseException(string message, int position) : base(message) => Position = position;
}
```

### Async Patterns
- Use `Task`-based async throughout
- **ALWAYS await Tasks** - Never use `.Result` or `.Wait()` - always `await`
- Use `ValueTask` for hot paths where allocation matters
- Test methods that return `Task` or `Task<T>` must be marked `async Task` and use `await`, never `.Wait()` or `.Result`

```csharp
public async Task<SipResponse> ProcessAsync(SipRequest request, CancellationToken ct)
{
    var dialog = await _factory.CreateAsync(request, ct);
    return await dialog.HandleAsync(request, ct);
}
```

**Anti-patterns to avoid:**
```csharp
// WRONG - blocks thread
var result = task.Result;
task.Wait();

// RIGHT - async all the way
var result = await task;
```

### Logging
- Use structured logging with message templates
- Include correlation IDs for tracing
- Use appropriate levels: Debug, Information, Warning, Error

```csharp
_logger.LogInformation(
    "Processing {Method} request for {CallId}",
    request.Method, dialog.CallId);

using (_logger.BeginScope("CallId:{CallId}", dialog.CallId)) { }
```

### SIP-Specific Conventions
- **URIs**: Use `SipUri` record type, not raw strings
- **Headers**: Use `IReadOnlyDictionary<string, string>` for immutability
- **Transaction state**: Follow RFC 3261 state machines exactly
- **Timers**: Use `TimeSpan` with explicit names (`TransactionTimeout`)

### Testing Guidelines
- Use **xUnit** as the test framework
- Name tests: `<MethodName>_<Scenario>_<ExpectedResult>`
- Use Arrange/Act/Assert structure
- Mock external dependencies (network, timers)

```csharp
[Fact]
public async Task Invite_Incoming_CreatesNewDialog()
{
    // Arrange
    var request = CreateInviteRequest();
    // Act
    var dialog = await _factory.CreateAsync(request);
    // Assert
    Assert.NotNull(dialog);
    Assert.Equal(DialogState.WaitingForAck, dialog.State);
}
```

### Architecture
- Dependency direction: App → Core → Transport
- Define small, focused interfaces (`ISipParser`, `ITransaction`)
- Prefer composition over inheritance
- Use `ConcurrentDictionary` for in-memory structures (Phase 2: 100k+ registrations, 20k+ dialogs)

---

## Commit Messages

```
feat(core): add dialog state machine transitions
fix(transport): handle malformed UDP packets gracefully
docs(api): document new IMediaSession interface
test(dialogs): add tests for INVITE transaction timeout
```

---

## RFC Documentation

**Source of Truth for SIP-related questions:**

The RFC 3261 documentation in `docs/rfcs/rfc3261/` contains detailed, searchable summaries of all SIP protocol concepts. Before implementing any SIP-related feature, consult these documents.

Key references:
- `docs/rfcs/rfc3261/index.yaml` - Searchable index
- `docs/rfcs/rfc3261/06-dialogs.md` - Dialog creation and B2BUA
- `docs/rfcs/rfc3261/11-transactions.md` - Transaction state machines
- `docs/rfcs/rfc3261/16-timer-values.md` - RFC timer defaults

---

## Additional Resources
- [API Surface](docs/api-surface.md)
- [Architecture Overview](docs/overview.md)
- [Concurrency Patterns](docs/concurrency.md)
- [Distributed Architecture](docs/distributed-architecture.md)


### Critical Rules

1. **Always use beads for multi-session work** - TodoWrite tasks are lost on compaction
2. **Update status when starting work** - Mark tasks as `in_progress`
3. **Close with completion reason** - Document what was accomplished
4. **Sync before ending session** - `bd sync` persists to git
5. **Check dependencies** - Use `bd dep tree <id>` to see blockers

### Phase Completion Celebration

**When completing a phase, add a Haiku to the commit message:**

A Haiku is a three-line Japanese form of poetry with a 5-7-5 syllable structure. It captures a moment or feeling related to the work completed.

**Example for completing Phase 1:**
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

**Example for completing Phase 2:**
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

**Guidelines:**
- Keep it work-related but have fun with it
- 5 syllables / 7 syllables / 5 syllables
- Add after the main commit message body
- Celebrate the milestone!

## Agent Instructions

- At the end of every development phase, create documentation in the `docs/internals/PROJECT_NAME/` directory
- Include an overview document with design, dependencies, and critical decisions
- For each namespace, create detailed architecture documentation
- Use the writing-clearly-and-concisely skill for documentation
- Use the mermaid-diagrams skill for creating diagrams
- Each PROJECT_NAME directory should have an Index.md with contents and deep links

## Landing the Plane (Session Completion)

**When ending a work session**, you MUST complete ALL steps below. Work is NOT complete until `git push` succeeds.

**MANDATORY WORKFLOW:**

1. **File issues for remaining work** - Create issues for anything that needs follow-up
2. **Run quality gates** (if code changed) - Tests, linters, builds
3. **Update issue status** - Close finished work, update in-progress items
4. **PUSH TO REMOTE** - This is MANDATORY:
   ```bash
   git pull --rebase
   bd sync
   git push
   git status  # MUST show "up to date with origin"
   ```
5. **Clean up** - Clear stashes, prune remote branches
6. **Verify** - All changes committed AND pushed
7. **Hand off** - Provide context for next session

**CRITICAL RULES:**
- Work is NOT complete until `git push` succeeds
- NEVER stop before pushing - that leaves work stranded locally
- NEVER say "ready to push when you are" - YOU must push
- If push fails, resolve and retry until it succeeds