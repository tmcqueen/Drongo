# Drongo — Public API Surface

## 1. Bootstrapping & Hosting

### DrongoApplication

```csharp
public sealed class DrongoApplication
{
    public static IDrongoBuilder CreateBuilder(string[] args);
}
```

### IDrongoBuilder

```csharp
public interface IDrongoBuilder
{
    IServiceCollection Services { get; }
    IConfiguration Configuration { get; }
    IHostEnvironment Environment { get; }

    IDrongoBuilder ConfigureHost(Action<IHostBuilder> configure);
    IDrongoBuilder ConfigureServices(Action<IServiceCollection> configure);

    IDrongoServerBuilder AddDrongoServer();
    IDrongoBuilder AddDrongoCore();
}
```

### IDrongoServerBuilder

```csharp
public interface IDrongoServerBuilder
{
    IDrongoServerBuilder ListenUdp(int port);
    IDrongoServerBuilder ListenUdp(IPAddress address, int port);

    // Phase 2
    IDrongoServerBuilder ListenTcp(int port);
    IDrongoServerBuilder ListenTcp(IPAddress address, int port);

    // Phase 3
    IDrongoServerBuilder ListenTls(int port, Action<TlsOptions> configure);

    IDrongoServerBuilder ConfigureTransport(Action<TransportOptions> configure);
}
```

### Build & Run

```csharp
public interface IDrongoApplication
{
    IServiceProvider Services { get; }

    void Use(Func<DrongoContext, Func<Task>, Task> middleware);

    IInviteEndpointConventionBuilder MapInvite(
        string pattern,
        Func<InviteContext, Task> handler);

    IRegisterEndpointConventionBuilder MapRegister(
        Func<RegisterContext, Task> handler);

    void Run();
    Task RunAsync(CancellationToken token = default);
}
```

---

## 2. Middleware Pipeline

The core design principle: SIP requests flow through a deterministic async pipeline.

### Delegate

```csharp
public delegate Task DrongoRequestDelegate(DrongoContext context);
```

### Middleware Signature

```csharp
public delegate Task DrongoMiddleware(
    DrongoContext context,
    DrongoRequestDelegate next);
```

### Registration

```csharp
app.Use(async (context, next) =>
{
    // Pre-processing
    await next(context);
    // Post-processing
});
```

---

## 3. Core Context Model

### DrongoContext (Base)

```csharp
public abstract class DrongoContext
{
    public SipRequest Request { get; }
    public SipResponse? Response { get; set; }

    public IServiceProvider RequestServices { get; }

    public CancellationToken CancellationToken { get; }

    public Task RespondAsync(int statusCode);
    public Task RespondAsync(int statusCode, string reasonPhrase);
}
```

### InviteContext

```csharp
public sealed class InviteContext : DrongoContext
{
    public IDialog Dialog { get; }
    public IMediaSession? Media { get; }

    public Task AnswerAsync();
    public Task RejectAsync(int statusCode);

    public Task TransferAsync(string targetUri);

    public Task<string?> GetDtmfAsync(
        TimeSpan? timeout = null);

    public Task HangupAsync();
}
```

### RegisterContext

```csharp
public sealed class RegisterContext : DrongoContext
{
    public string AddressOfRecord { get; }
    public IReadOnlyList<ContactBinding> Contacts { get; }

    public Task AcceptAsync();
    public Task RejectAsync(int statusCode);
}
```

---

## 4. SIP Abstractions

### SipRequest

```csharp
public sealed class SipRequest
{
    public string Method { get; }
    public SipUri RequestUri { get; }

    public IReadOnlyDictionary<string, string> Headers { get; }

    public ReadOnlyMemory<byte> Body { get; }
}
```

### SipResponse

```csharp
public sealed class SipResponse
{
    public int StatusCode { get; }
    public string ReasonPhrase { get; }

    public IReadOnlyDictionary<string, string> Headers { get; }
    public ReadOnlyMemory<byte> Body { get; }
}
```

### IDialog

```csharp
public interface IDialog
{
    string CallId { get; }
    string LocalTag { get; }
    string RemoteTag { get; }

    DialogState State { get; }

    Task SendRequestAsync(SipRequest request);
    Task TerminateAsync();
}
```

---

## 5. Registrar API

### IRegistrar

```csharp
public interface IRegistrar
{
    Task RegisterAsync(RegisterRequest request);
    Task<IReadOnlyList<ContactBinding>> ResolveAsync(string aor);
    Task UnregisterAsync(string aor, string contact);
}
```

### RegisterRequest

```csharp
public sealed class RegisterRequest
{
    public string AddressOfRecord { get; }
    public IReadOnlyList<ContactBinding> Contacts { get; }
    public TimeSpan Expires { get; }
}
```

### ContactBinding

```csharp
public sealed class ContactBinding
{
    public string ContactUri { get; }
    public DateTimeOffset ExpiresAt { get; }
    public string? InstanceId { get; }
}
```

---

## 6. Media Abstractions

*Phase 1: Minimal, IVR-focused.*

### IMediaSession

```csharp
public interface IMediaSession
{
    Task StartAsync();
    Task PlayAsync(string filePath);
    Task RecordAsync(string filePath);
    Task<string?> ReceiveDtmfAsync(
        TimeSpan? timeout = null);

    Task StopAsync();
}
```

### IMediaSessionFactory

```csharp
public interface IMediaSessionFactory
{
    Task<IMediaSession> CreateAsync(
        InviteContext context);
}
```

---

## 7. Configuration API

### Extension Methods

```csharp
public static class DrongoConfigurationExtensions
{
    public static IServiceCollection AddDrongoConfiguration(
        this IServiceCollection services);

    public static IServiceCollection ConfigureSip(
        this IServiceCollection services,
        Action<SipOptions> configure);
}
```

### SipOptions

```csharp
public sealed class SipOptions
{
    public int MaxDialogs { get; set; }
    public TimeSpan TransactionTimeout { get; set; }
    public bool EnableLooseRouting { get; set; }
}
```

---

## 8. Scripting / IVR Extensions

We avoid a heavy DSL initially. Just composable async handlers.

### Invite Mapping

```csharp
public interface IInviteEndpointConventionBuilder
{
    IInviteEndpointConventionBuilder RequirePolicy(string name);
}
```

### Example

```csharp
app.MapInvite("/support", async call =>
{
    await call.AnswerAsync();
    await call.Media!.PlayAsync("welcome.wav");

    var digit = await call.GetDtmfAsync();

    if (digit == "1")
        await call.TransferAsync("sip:sales@example.com");

    await call.HangupAsync();
});
```

---

## 9. Observability

### IDrongoMetrics

```csharp
public interface IDrongoMetrics
{
    long ActiveDialogs { get; }
    long ActiveTransactions { get; }
    long RegisteredUsers { get; }
}
```

### Logging

Fully integrated with `ILogger<T>`. Per-dialog scope:

```csharp
using (_logger.BeginScope("CallId:{CallId}", dialog.CallId))
{
    // All logs in this scope include CallId
}
```

---

## 10. Future-Proofing Hooks (Reserved)

These are not implemented in Phase 1 but designed for expansion:

```csharp
public interface IWebSocketTransport { }
public interface IWebRtcSession { }
public interface ISdpNegotiator { }
public interface ISrtpSession { }
```

---

## Design Characteristics

- **No static state** — Everything DI-driven
- **Transport isolated** from Media
- **Middleware-first** routing
- **Deterministic** transaction model
- **Context objects** are short-lived and request-scoped

---

## Summary

This API surface:
- Feels native to .NET developers
- Mirrors ASP.NET Core hosting patterns
- Is minimal but expandable
- Keeps SIP internals hidden behind abstractions
- Leaves space for WebRTC and Admin in Phase 3
