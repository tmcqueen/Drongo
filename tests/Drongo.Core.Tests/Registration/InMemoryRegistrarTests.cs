using Drongo.Core.Messages;
using Drongo.Core.Registration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

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
        result.Bindings.Count.ShouldBe(1);
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
        result.Bindings.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Register_MultipleContacts_StoresAll()
    {
        var request = CreateRegisterRequest("sip:carol@example.com", 
            "<sip:carol@192.0.2.3:5060>,<sip:carol@192.0.2.4:5060>");

        var result = await _registrar.RegisterAsync(request);

        result.IsSuccess.ShouldBeTrue();
        result.Bindings.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Unregister_SpecificContact_RemovesOnlyThatContact()
    {
        await _registrar.RegisterAsync(CreateRegisterRequest("sip:dave@example.com", 
            "<sip:dave@192.0.2.5:5060>,<sip:dave@192.0.2.6:5060>"));

        var unregisterRequest = CreateRegisterRequest("sip:dave@example.com", "<sip:dave@192.0.2.5:5060>;expires=0");
        var result = await _registrar.UnregisterAsync(unregisterRequest);

        result.Bindings.Count.ShouldBe(1);
        result.Bindings[0].ContactUri.ToString().ShouldContain("192.0.2.6");
    }

    [Fact]
    public async Task Unregister_AllContacts_RemovesAor()
    {
        await _registrar.RegisterAsync(CreateRegisterRequest("sip:eve@example.com", "<sip:eve@192.0.2.7:5060>"));

        var unregisterRequest = CreateRegisterRequest("sip:eve@example.com", "*");
        var result = await _registrar.UnregisterAsync(unregisterRequest);

        result.Bindings.Count.ShouldBe(0);
        
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

        result.Bindings[0].ExpiresAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddMinutes(50));
    }

    [Fact]
    public async Task Register_WithExpiresParameter_UsesProvidedValue()
    {
        var request = CreateRegisterRequest("sip:henry@example.com", "<sip:henry@192.0.2.11:5060>;expires=1800");
        var result = await _registrar.RegisterAsync(request);

        result.Bindings[0].ExpiresAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddMinutes(29));
        result.Bindings[0].ExpiresAt.ShouldBeLessThan(DateTimeOffset.UtcNow.AddMinutes(31));
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
