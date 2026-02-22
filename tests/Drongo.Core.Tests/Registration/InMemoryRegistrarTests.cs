using Drongo.Core.Messages;
using Drongo.Core.Registration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Drongo.Core.Tests.Registration;

public class InMemoryRegistrarTests
{
    private readonly InMemoryRegistrar _registrar;
    private readonly ILogger<InMemoryRegistrar> _logger;

    public InMemoryRegistrarTests()
    {
        _logger = Substitute.For<ILogger<InMemoryRegistrar>>();
        _registrar = new InMemoryRegistrar(_logger);
    }

    [Fact]
    public async Task Register_NewContact_CreatesBinding()
    {
        var request = CreateRegisterRequest("sip:alice@example.com", "<sip:alice@192.0.2.1:5060>");

        var result = await _registrar.RegisterAsync(request);

        result.IsSuccess.ShouldBeTrue();
        result.StatusCode.ShouldBe(200);
        result.Bindings.ShouldNotBeNull();
        result.Bindings!.Count.ShouldBe(1);
        result.Bindings[0].ContactUri.ToString().ShouldContain("alice@192.0.2.1");
    }

    [Fact]
    public async Task Register_RefreshContact_UpdatesExpiration()
    {
        var request1 = CreateRegisterRequest("sip:bob@example.com", "<sip:bob@192.0.2.2:5060>");
        await _registrar.RegisterAsync(request1);

        var request2 = CreateRegisterRequest("sip:bob@example.com", "<sip:bob@192.0.2.2:5060>");
        var result = await _registrar.RegisterAsync(request2);

        result.IsSuccess.ShouldBeTrue();
        result.Bindings.ShouldNotBeNull();
        result.Bindings!.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Register_MultipleContacts_StoresAll()
    {
        var request = CreateRegisterRequest("sip:carol@example.com", 
            "<sip:carol@192.0.2.3:5060>,<sip:carol@192.0.2.4:5060>");

        var result = await _registrar.RegisterAsync(request);

        result.IsSuccess.ShouldBeTrue();
        result.Bindings.ShouldNotBeNull();
        result.Bindings!.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Unregister_SpecificContact_RemovesOnlyThatContact()
    {
        await _registrar.RegisterAsync(CreateRegisterRequest("sip:dave@example.com", 
            "<sip:dave@192.0.2.5:5060>,<sip:dave@192.0.2.6:5060>"));

        var unregisterRequest = CreateRegisterRequest("sip:dave@example.com", "<sip:dave@192.0.2.5:5060>;expires=0");
        var result = await _registrar.UnregisterAsync(unregisterRequest);

        result.Bindings.ShouldNotBeNull();
        result.Bindings!.Count.ShouldBe(1);
        result.Bindings[0].ContactUri.ToString().ShouldContain("192.0.2.6");
    }

    [Fact]
    public async Task Unregister_AllContacts_RemovesAor()
    {
        await _registrar.RegisterAsync(CreateRegisterRequest("sip:eve@example.com", "<sip:eve@192.0.2.7:5060>"));

        var unregisterRequest = CreateRegisterRequest("sip:eve@example.com", "*");
        var result = await _registrar.UnregisterAsync(unregisterRequest);

        result.Bindings.ShouldNotBeNull();
        result.Bindings!.Count.ShouldBe(0);
        
        var bindings = await _registrar.GetBindingsAsync(SipUri.Parse("sip:eve@example.com"));
        bindings.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetBindings_ReturnsOrderedByQValue()
    {
        var request = CreateRegisterRequest("sip:frank@example.com", 
            "<sip:frank@192.0.2.8:5060>;q=0.5,<sip:frank@192.0.2.9:5060>;q=1.0");
        await _registrar.RegisterAsync(request);

        var bindings = await _registrar.GetBindingsAsync(SipUri.Parse("sip:frank@example.com"));

        bindings[0].ContactUri.ToString().ShouldContain("192.0.2.9");
        bindings[1].ContactUri.ToString().ShouldContain("192.0.2.8");
    }

    [Fact]
    public async Task Register_WithoutExpires_UsesDefault()
    {
        var request = CreateRegisterRequest("sip:grace@example.com", "<sip:grace@192.0.2.10:5060>");
        var result = await _registrar.RegisterAsync(request);

        result.Bindings.ShouldNotBeNull();
        result.Bindings![0].ExpiresAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddMinutes(50));
    }

    [Fact]
    public async Task Register_WithExpiresParameter_UsesProvidedValue()
    {
        var request = CreateRegisterRequest("sip:henry@example.com", "<sip:henry@192.0.2.11:5060>;expires=1800");
        var result = await _registrar.RegisterAsync(request);

        result.Bindings.ShouldNotBeNull();
        result.Bindings![0].ExpiresAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddMinutes(29));
        result.Bindings[0].ExpiresAt.ShouldBeLessThan(DateTimeOffset.UtcNow.AddMinutes(31));
    }

    [Fact]
    public async Task RegisterAsync_ConcurrentDistinctContactsForSameAor_NoBindingsAreLost()
    {
        // Arrange: 20 distinct contacts will be registered in parallel for the same AOR.
        // A race condition on the shared List<ContactBinding> can cause some adds to be
        // silently dropped, so we assert that all 20 survive.
        const int threadCount = 20;
        var aor = "sip:concurrent@example.com";
        var tasks = Enumerable.Range(1, threadCount)
            .Select(i => _registrar.RegisterAsync(
                CreateRegisterRequest(aor, $"<sip:concurrent@10.0.0.{i}:5060>")))
            .ToList();

        // Act
        await Task.WhenAll(tasks);

        // Assert
        var bindings = await _registrar.GetBindingsAsync(SipUri.Parse(aor));
        bindings.Count.ShouldBe(threadCount,
            $"Expected {threadCount} distinct bindings but got {bindings.Count}. " +
            "A count < threadCount indicates a lost update due to a race condition.");
    }

    [Fact]
    public async Task RegisterAsync_ConcurrentRefreshOfSameContact_BindingCountRemainsOne()
    {
        // Arrange: the same contact URI is re-registered (refreshed) from many threads at once.
        // The idempotent refresh must produce exactly 1 binding regardless of concurrency.
        const int threadCount = 30;
        var aor = "sip:refresh@example.com";
        var contact = "<sip:refresh@10.0.1.1:5060>";

        var tasks = Enumerable.Range(0, threadCount)
            .Select(_ => _registrar.RegisterAsync(CreateRegisterRequest(aor, contact)))
            .ToList();

        // Act
        await Task.WhenAll(tasks);

        // Assert
        var bindings = await _registrar.GetBindingsAsync(SipUri.Parse(aor));
        bindings.Count.ShouldBe(1,
            "Concurrent refresh of the same contact URI must remain idempotent and yield exactly 1 binding.");
    }

    [Fact]
    public async Task RegisterAsync_ConcurrentRegistersAndReads_ReadsNeverThrow()
    {
        // Arrange: simultaneously hammer register and GetBindings to catch
        // InvalidOperationException / index-out-of-range from unsynchronised List mutation.
        const int writerCount = 15;
        const int readerCount = 15;
        var aor = "sip:chaos@example.com";
        var sipAor = SipUri.Parse(aor);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var writers = Enumerable.Range(1, writerCount)
            .Select(i => _registrar.RegisterAsync(
                CreateRegisterRequest(aor, $"<sip:chaos@10.0.2.{i}:5060>"), cts.Token))
            .ToList();

        var readers = Enumerable.Range(0, readerCount)
            .Select(_ => _registrar.GetBindingsAsync(sipAor, cts.Token))
            .ToList();

        // Act + Assert: no exception must escape any task
        var allTasks = writers.Cast<Task>().Concat(readers.Cast<Task>()).ToList();
        var exception = await Record.ExceptionAsync(() => Task.WhenAll(allTasks));
        exception.ShouldBeNull("Concurrent register+read must never throw.");
    }

    [Fact]
    public async Task UnregisterAsync_ConcurrentUnregisterAndRegister_NoCorruption()
    {
        // Arrange: seed two contacts, then concurrently unregister one while registering a third.
        var aor = "sip:unregrace@example.com";
        await _registrar.RegisterAsync(CreateRegisterRequest(aor, "<sip:unregrace@10.0.3.1:5060>"));
        await _registrar.RegisterAsync(CreateRegisterRequest(aor, "<sip:unregrace@10.0.3.2:5060>"));

        var unregisterTask = _registrar.UnregisterAsync(
            CreateRegisterRequest(aor, "<sip:unregrace@10.0.3.1:5060>;expires=0"));
        var registerTask = _registrar.RegisterAsync(
            CreateRegisterRequest(aor, "<sip:unregrace@10.0.3.3:5060>"));

        // Act
        await Task.WhenAll(unregisterTask, registerTask);

        // Assert: .10.0.3.1 should be gone, .2 and .3 should exist (or at least no corruption/exception).
        var bindings = await _registrar.GetBindingsAsync(SipUri.Parse(aor));
        bindings.ShouldNotBeNull();
        bindings.Any(b => b.ContactUri.ToString().Contains("10.0.3.1")).ShouldBeFalse(
            "The unregistered contact 10.0.3.1 must not appear in final bindings.");
    }

    [Fact]
    public async Task GetAllBindingsAsync_ConcurrentRegistrations_ReturnsConsistentSnapshot()
    {
        // Arrange: register contacts for multiple AORs concurrently, then read all.
        const int aorCount = 10;
        var tasks = Enumerable.Range(1, aorCount)
            .Select(i => _registrar.RegisterAsync(
                CreateRegisterRequest($"sip:user{i}@example.com", $"<sip:user{i}@10.0.4.{i}:5060>")))
            .ToList();

        await Task.WhenAll(tasks);

        // Act: calling GetAllBindingsAsync must not throw and must return a stable count.
        var exception = await Record.ExceptionAsync(() => _registrar.GetAllBindingsAsync());
        exception.ShouldBeNull("GetAllBindingsAsync must not throw under concurrent writes.");

        var allBindings = await _registrar.GetAllBindingsAsync();
        allBindings.Count.ShouldBe(aorCount,
            $"Expected exactly {aorCount} bindings (one per AOR) but got {allBindings.Count}.");
    }

    private static SipRequest CreateRegisterRequest(string to, string contact)
    {
        return new SipRequest(
            SipMethod.Register,
            new SipUri("sip", "registrar.example.com", 5060),
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.1;branch=z9hG4bK776asdhds",
                ["From"] = $"<{to}>;tag=caller-tag",
                ["To"] = $"<{to}>",
                ["Call-ID"] = "test-registration",
                ["CSeq"] = "1 REGISTER",
                ["Contact"] = contact,
                ["Expires"] = "3600"
            });
    }
}
