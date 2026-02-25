# Code Style Guidelines

## General Principles

- **No static state** — Everything must be dependency-injected
- **Deterministic transactions** — SIP state machines must be predictable
- **Transport isolation** — Core SIP engine remains transport-neutral
- **Request-scoped context** — Context objects are short-lived per request

---

## Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Interfaces | `I` prefix | `ISipChannel`, `IDialog` |
| Classes | PascalCase | `SipRequest`, `InviteContext` |
| Methods | PascalCase | `SendRequestAsync`, `ResolveAsync` |
| Properties | PascalCase | `CallId`, `RequestUri` |
| Private fields | `_camelCase` | `_registrar`, `_transport` |
| Constants | PascalCase | `DefaultPort = 5060` |
| Enums | PascalCase | `DialogState`, `SipMethod` |

---

## Directory Structure

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

---

## File Organization

### Imports

- Use **global usings** in `GlobalUsings.cs`
- Order: `System.*`, third-party, project-specific

```csharp
global using System.Net;
global using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Drongo.Core.Dialogs;
```

---

## Types & Nullability

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

---

## Error Handling

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

---

## Async Patterns

### CRITICAL: Always Await Tasks

- Use `Task`-based async throughout
- **ALWAYS await Tasks** — Never use `.Result` or `.Wait()` — always `await`
- Use `ValueTask` for hot paths where allocation matters
- Test methods that return `Task` or `Task<T>` must be marked `async Task` and use `await`, never `.Wait()` or `.Result`

```csharp
public async Task<SipResponse> ProcessAsync(SipRequest request, CancellationToken ct)
{
    var dialog = await _factory.CreateAsync(request, ct);
    return await dialog.HandleAsync(request, ct);
}
```

### Anti-patterns to Avoid

```csharp
// WRONG - blocks thread, causes deadlocks
var result = task.Result;
task.Wait();

// RIGHT - async all the way
var result = await task;
```

---

## Logging

- Use structured logging with message templates
- Include correlation IDs for tracing
- Use appropriate levels: Debug, Information, Warning, Error

```csharp
_logger.LogInformation(
    "Processing {Method} request for {CallId}",
    request.Method, dialog.CallId);

using (_logger.BeginScope("CallId:{CallId}", dialog.CallId)) { }
```

---

## SIP-Specific Conventions

- **URIs**: Use `SipUri` record type, not raw strings
- **Headers**: Use `IReadOnlyDictionary<string, string>` for immutability
- **Transaction state**: Follow RFC 3261 state machines exactly
- **Timers**: Use `TimeSpan` with explicit names (`TransactionTimeout`)

---

## Architecture

- Dependency direction: App → Core → Transport
- Define small, focused interfaces (`ISipParser`, `ITransaction`)
- Prefer composition over inheritance
- Use `ConcurrentDictionary` for in-memory structures (Phase 2: 100k+ registrations, 20k+ dialogs)
