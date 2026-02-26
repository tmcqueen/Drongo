using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Drongo.Core.SIP.Parsing;
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

/// <summary>
/// Tests verifying that each SocketAsyncEventArgs owns its own dedicated receive buffer
/// (Drongo-umu: shared-buffer race condition on multi-core machines).
/// </summary>
public class UdpTransport_ReceiveBufferIsolation_Tests : IAsyncDisposable
{
    private readonly UdpTransport _transport;
    private readonly ISipParser _parser;
    private readonly ILogger<UdpTransport> _logger;
    private readonly IUdpTransportFactory _factory;

    public UdpTransport_ReceiveBufferIsolation_Tests()
    {
        _parser = Substitute.For<ISipParser>();
        _parser.ParseRequest(default).ReturnsForAnyArgs(SipRequestParseResult.Failure("test"));
        _parser.ParseResponse(default).ReturnsForAnyArgs(SipResponseParseResult.Failure("test"));

        _logger = Substitute.For<ILogger<UdpTransport>>();
        _factory = Substitute.For<IUdpTransportFactory>();

        // Use port 0 so the OS assigns a free port.
        // receiveBufferSize=512 to keep memory footprint small in tests.
        _transport = new UdpTransport(_factory, _parser, _logger, 0, receiveBufferSize: 512);
    }

    public async ValueTask DisposeAsync()
    {
        await _transport.DisposeAsync();
    }

    /// <summary>
    /// Confirms that UdpTransport no longer holds a single shared _buffer field.
    /// If the field still exists the bug is present; the test must fail before the fix.
    /// </summary>
    [Fact]
    public void Constructor_SharedBufferField_DoesNotExist()
    {
        var field = typeof(UdpTransport).GetField(
            "_buffer",
            BindingFlags.NonPublic | BindingFlags.Instance);

        field.ShouldBeNull("_buffer shared field must be removed; each SocketAsyncEventArgs must own its own buffer");
    }

    /// <summary>
    /// After StartAsync, every SocketAsyncEventArgs in _receiveArgs must have its own
    /// distinct byte[] so concurrent receives cannot overwrite each other.
    /// </summary>
    [Fact]
    public async Task StartAsync_MultipleReceiveArgs_EachArgHasDistinctBuffer()
    {
        await _transport.StartAsync(CancellationToken.None);

        var receiveArgsField = typeof(UdpTransport).GetField(
            "_receiveArgs",
            BindingFlags.NonPublic | BindingFlags.Instance);

        receiveArgsField.ShouldNotBeNull("_receiveArgs field must exist");

        var receiveArgs = (List<SocketAsyncEventArgs>)receiveArgsField!.GetValue(_transport)!;

        receiveArgs.Count.ShouldBeGreaterThan(0, "at least one SocketAsyncEventArgs must be created");

        // Collect all buffer references; all must be distinct objects.
        var buffers = receiveArgs
            .Select(a => a.Buffer)
            .ToList();

        // Every buffer must be non-null and the expected size.
        buffers.ShouldAllBe(b => b != null, "every args must have a non-null buffer");
        buffers.ShouldAllBe(b => b!.Length == 512, "every args buffer must match receiveBufferSize");

        // No two args may share the same array reference.
        var distinctBuffers = buffers.Distinct(ReferenceEqualityComparer.Instance).Count();
        distinctBuffers.ShouldBe(
            receiveArgs.Count,
            "each SocketAsyncEventArgs must own a unique buffer array — sharing causes race conditions on multi-core machines");
    }

    /// <summary>
    /// When more than one SocketAsyncEventArgs is created (ProcessorCount > 1 scenario),
    /// mutating one buffer must not affect any other buffer.
    /// </summary>
    [Fact]
    public async Task StartAsync_MultipleReceiveArgs_MutatingOneBufferDoesNotAffectOthers()
    {
        // Force at least 2 args by using a transport with receiveCount derived from 2+ processors.
        // We simulate by starting the real transport (port 0) and then examining the args.
        await _transport.StartAsync(CancellationToken.None);

        var receiveArgsField = typeof(UdpTransport).GetField(
            "_receiveArgs",
            BindingFlags.NonPublic | BindingFlags.Instance);

        var receiveArgs = (List<SocketAsyncEventArgs>)receiveArgsField!.GetValue(_transport)!;

        if (receiveArgs.Count < 2)
        {
            // Single-core machine: cannot demonstrate the race but the isolation still holds.
            // At minimum the one buffer must not be null.
            receiveArgs[0].Buffer.ShouldNotBeNull();
            return;
        }

        // Zero-fill first buffer, fill second buffer with 0xFF.
        Array.Fill(receiveArgs[0].Buffer!, (byte)0x00);
        Array.Fill(receiveArgs[1].Buffer!, (byte)0xFF);

        // First buffer must still be all zeros — not corrupted by the write to the second.
        receiveArgs[0].Buffer!.ShouldAllBe(b => b == 0x00,
            "mutating args[1].Buffer must not affect args[0].Buffer — they must be independent arrays");
    }
}
