using System.Collections.Concurrent;
using Drongo.Core.SIP.Messages;

namespace Drongo.Core.SIP.Registration;

public interface IRegistrar
{
    Task<RegistrationResult> RegisterAsync(SipRequest request, CancellationToken ct = default);
    Task<RegistrationResult> UnregisterAsync(SipRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ContactBinding>> GetBindingsAsync(SipUri aor, CancellationToken ct = default);
    Task<IReadOnlyList<ContactBinding>> GetAllBindingsAsync(CancellationToken ct = default);
}

public record RegistrationResult(
    bool IsSuccess,
    int StatusCode,
    string ReasonPhrase,
    IReadOnlyList<ContactBinding>? Bindings = null);
