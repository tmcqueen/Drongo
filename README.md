# Drongo

A .NET-native SIP (Session Initiation Protocol) runtime and softswitch built for high performance and reliability.

## What is Drongo?

Drongo implements a complete SIP stack following RFC 3261 specifications. It provides a deterministic, message-driven foundation for building VoIP applications, PBX systems, and telephony services.

## Features

### Core Signaling
- **Dialog Layer** - Full B2BUA dialog state machine handling INVITE, ACK, BYE, and re-INVITE
- **Transaction Management** - RFC 3261 client and server transaction state machines
- **SIP Parser** - Stateless, high-performance message parsing

### Transport
- **UDP Transport** - Production-ready UDP listener with SocketAsyncEventArgs for optimal throughput
- **Extensible Design** - TCP/TLS transport ready for Phase 2

### Registration
- **In-Memory Registrar** - ConcurrentDictionary-based bindings with expiration management
- **Standards Compliant** - Full support for REGISTER, Contact bindings, and wildcards

### Media
- **NAudio Integration** - Audio session management for playback and recording
- **DTMF Support** - RFC 2833 DTMF event handling

### Hosting
- **ASP.NET Core Patterns** - IDrongoBuilder for fluent configuration
- **Middleware Pipeline** - Route INVITE and REGISTER requests with custom handlers

## Architecture

```
Drongo/
├── Drongo.Core/       # Core SIP engine
│   ├── Dialogs/       # RFC 3261 dialog state machine
│   ├── Transactions/   # Client & server transactions
│   ├── Transport/     # UDP transport layer
│   ├── Registration/ # In-memory registrar
│   ├── Messages/     # SIP data types
│   ├── Parsing/      # Message parser
│   └── Timers/       # RFC timer infrastructure
├── Drongo.Media/      # NAudio media sessions
└── Drongo/           # Application host
```

## Requirements

- .NET 10+
- C# 13+

## Quick Start

```bash
# Clone and build
dotnet build

# Run the server
dotnet run --project src/Drongo/Drongo.csproj
```

## Configuration

```csharp
var builder = DrongoApplication.CreateBuilder();

// Register handler for INVITE requests
builder.MapInvite(async context =>
{
    await context.SendResponseAsync(180, "Ringing");
    await context.SendResponseAsync(200, "OK");
});

// Register handler for REGISTER requests  
builder.MapRegister(async context =>
{
    // Process registration
    await context.SendResponseAsync(200, "OK");
});
```

## Testing

```bash
# Run all tests
dotnet test

# Run with code coverage
dotnet test /p:CollectCoverage=true
```

**93 unit tests** covering dialogs, transactions, transport, and registration.

## Roadmap

| Phase | Focus |
|-------|-------|
| Phase 1 | Core Signaling MVP |
| Phase 2 | TCP Transport, Media Bridging |
| Phase 3 | WebRTC, Admin Framework |

## Documentation

- [Architecture Overview](docs/overview.md)
- [RFC 3261 Documentation](docs/rfcs/rfc3261/)
- [API Surface](docs/api-surface.md)

## License

MIT
