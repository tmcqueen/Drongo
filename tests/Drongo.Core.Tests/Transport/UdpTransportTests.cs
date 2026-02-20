using System.Net;
using Drongo.Core.Parsing;
using Drongo.Core.Transport;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Drongo.Core.Tests.Transport;

public class UdpTransportFactoryTests
{
    private readonly UdpTransportFactory _factory;
    private readonly ISipParser _parser;
    private readonly ILoggerFactory _loggerFactory;

    public UdpTransportFactoryTests()
    {
        _parser = Substitute.For<ISipParser>();
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger<UdpTransport>().Returns(Substitute.For<ILogger<UdpTransport>>());
        
        _factory = new UdpTransportFactory(_parser, _loggerFactory);
    }

    [Fact]
    public void Create_WithDefaultAddress_ReturnsTransport()
    {
        var transport = _factory.Create(5060);

        transport.ShouldNotBeNull();
    }

    [Fact]
    public void Create_WithSpecificAddress_ReturnsTransport()
    {
        var address = IPAddress.Parse("127.0.0.1");
        var transport = _factory.Create(5060, address);

        transport.ShouldNotBeNull();
    }

    [Fact]
    public void Create_MultipleTransports_ReturnsDifferentInstances()
    {
        var transport1 = _factory.Create(5060);
        var transport2 = _factory.Create(5061);

        transport1.ShouldNotBeSameAs(transport2);
    }
}

public class UdpTransportTests
{
    private readonly UdpTransport _transport;
    private readonly ISipParser _parser;
    private readonly ILogger<UdpTransport> _logger;
    private readonly IUdpTransportFactory _factory;

    public UdpTransportTests()
    {
        _parser = Substitute.For<ISipParser>();
        _parser.ParseRequest(default).ReturnsForAnyArgs(SipRequestParseResult.Failure("test"));
        _parser.ParseResponse(default).ReturnsForAnyArgs(SipResponseParseResult.Failure("test"));
        
        _logger = Substitute.For<ILogger<UdpTransport>>();
        _factory = Substitute.For<IUdpTransportFactory>();
        
        _transport = new UdpTransport(_factory, _parser, _logger, 0);
    }

    [Fact]
    public void IsRunning_BeforeStart_ReturnsFalse()
    {
        _transport.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public void ImplementsIDisposable()
    {
        _transport.ShouldBeAssignableTo<IDisposable>();
    }

    [Fact]
    public void ImplementsIAsyncDisposable()
    {
        _transport.ShouldBeAssignableTo<IAsyncDisposable>();
    }

    [Fact]
    public async Task SendResponseAsync_NotRunning_Throws()
    {
        var data = new byte[] { 0x00 };
        
        var act = () => _transport.SendResponseAsync(data, new IPEndPoint(IPAddress.Loopback, 5060));
        
        await act.ShouldThrowAsync<InvalidOperationException>();
    }
}
