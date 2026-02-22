using System.Collections.Concurrent;
using System.Collections.Immutable;
using Drongo.Core.Messages;
using Microsoft.Extensions.Logging;

namespace Drongo.Core.Registration;

public sealed class InMemoryRegistrar : IRegistrar
{
    private readonly ConcurrentDictionary<string, ImmutableList<ContactBinding>> _bindings = new();
    private readonly ILogger<InMemoryRegistrar> _logger;
    private const int DefaultExpires = 3600;
    private const int MaxExpires = 7200;

    public InMemoryRegistrar(ILogger<InMemoryRegistrar> logger)
    {
        _logger = logger;
    }

    public Task<RegistrationResult> RegisterAsync(SipRequest request, CancellationToken ct = default)
    {
        var aor = request.To;

        if (string.IsNullOrEmpty(aor))
        {
            return Task.FromResult(new RegistrationResult(false, 400, "Missing To header"));
        }

        var aorUri = SipUri.Parse(aor);
        var aorKey = aorUri.ToString().ToLowerInvariant();

        var contacts = ParseContacts(request);

        if (contacts.Count == 0)
        {
            return Task.FromResult(new RegistrationResult(false, 400, "Missing Contact header"));
        }

        var updated = _bindings.AddOrUpdate(
            aorKey,
            _ => ImmutableList.CreateRange(contacts),
            (_, existing) => MergeContacts(existing, contacts));

        var bindings = updated
            .Where(b => !b.IsExpired)
            .OrderByDescending(b => b.QValue ?? 1.0f)
            .ToList();

        _logger.LogInformation("Registered {Count} contacts for AOR {Aor}", bindings.Count, aorKey);

        return Task.FromResult(new RegistrationResult(true, 200, "OK", bindings));
    }

    public Task<RegistrationResult> UnregisterAsync(SipRequest request, CancellationToken ct = default)
    {
        var aor = request.To;

        if (string.IsNullOrEmpty(aor))
        {
            return Task.FromResult(new RegistrationResult(false, 400, "Missing To header"));
        }

        var aorUri = SipUri.Parse(aor);
        var aorKey = aorUri.ToString().ToLowerInvariant();

        if (request.Contact == "*")
        {
            _bindings.TryRemove(aorKey, out _);
            _logger.LogInformation("Unregistered all contacts for AOR {Aor}", aorKey);
            return Task.FromResult(new RegistrationResult(true, 200, "OK", []));
        }

        var contacts = ParseContacts(request);

        var remaining = _bindings.AddOrUpdate(
            aorKey,
            _ => ImmutableList<ContactBinding>.Empty,
            (_, existing) =>
            {
                var updated = existing;
                foreach (var contact in contacts)
                {
                    updated = updated.RemoveAll(c =>
                        c.ContactUri.ToString().Equals(
                            contact.ContactUri.ToString(),
                            StringComparison.OrdinalIgnoreCase));
                }
                return updated;
            });

        var bindings = remaining
            .Where(b => !b.IsExpired)
            .ToList();

        return Task.FromResult(new RegistrationResult(true, 200, "OK", bindings));
    }

    public Task<IReadOnlyList<ContactBinding>> GetBindingsAsync(SipUri aor, CancellationToken ct = default)
    {
        var aorKey = aor.ToString().ToLowerInvariant();

        if (_bindings.TryGetValue(aorKey, out var bindings))
        {
            var valid = bindings
                .Where(b => !b.IsExpired)
                .OrderByDescending(b => b.QValue ?? 1.0f)
                .ToList();
            return Task.FromResult<IReadOnlyList<ContactBinding>>(valid);
        }

        return Task.FromResult<IReadOnlyList<ContactBinding>>(Array.Empty<ContactBinding>());
    }

    public Task<IReadOnlyList<ContactBinding>> GetAllBindingsAsync(CancellationToken ct = default)
    {
        var all = _bindings
            .SelectMany(kvp => kvp.Value)
            .Where(b => !b.IsExpired)
            .OrderByDescending(b => b.QValue ?? 1.0f)
            .ToList();

        return Task.FromResult<IReadOnlyList<ContactBinding>>(all);
    }

    // Pure function — builds a new ImmutableList from existing + incoming contacts.
    // Called inside AddOrUpdate; must not mutate either argument.
    private static ImmutableList<ContactBinding> MergeContacts(
        ImmutableList<ContactBinding> existing,
        IReadOnlyList<ContactBinding> incoming)
    {
        // Start from non-expired existing bindings.
        var result = existing.RemoveAll(c => c.IsExpired);

        foreach (var contact in incoming)
        {
            var idx = result.FindIndex(c =>
                c.ContactUri.ToString().Equals(
                    contact.ContactUri.ToString(),
                    StringComparison.OrdinalIgnoreCase));

            result = idx >= 0
                ? result.SetItem(idx, contact)   // refresh / update existing
                : result.Add(contact);            // new binding
        }

        return result;
    }

    private int GetExpires(SipRequest request)
    {
        var expiresHeader = request.TryGetHeader("Expires");
        if (expiresHeader != null && int.TryParse(expiresHeader, out var expires))
        {
            return Math.Min(Math.Max(expires, 0), MaxExpires);
        }
        return DefaultExpires;
    }

    private List<ContactBinding> ParseContacts(SipRequest request)
    {
        var contacts = new List<ContactBinding>();
        var contactHeader = request.Contact;

        if (string.IsNullOrEmpty(contactHeader))
        {
            return contacts;
        }

        var expires = GetExpires(request);

        if (contactHeader == "*")
        {
            contacts.Add(new ContactBinding(new SipUri("sip", "*", 0), DateTimeOffset.MinValue));
            return contacts;
        }

        foreach (var contact in contactHeader.Split(','))
        {
            var trimmed = contact.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                var binding = ContactBinding.Parse(trimmed);
                if (binding.ExpiresAt == DateTimeOffset.MaxValue)
                {
                    binding = new ContactBinding(
                        binding.ContactUri,
                        DateTimeOffset.UtcNow.AddSeconds(expires),
                        binding.InstanceId,
                        binding.QValue,
                        binding.Methods);
                }
                contacts.Add(binding);
            }
        }

        return contacts;
    }
}
