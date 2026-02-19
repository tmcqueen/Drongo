using Drongo.Core.Timers;
using Xunit;

namespace Drongo.Core.Tests.Timers;

public class SipTimerFactoryTests
{
    private readonly SipTimerFactory _factory = new();

    [Fact]
    public void T1_Default_Returns500Ms()
    {
        Assert.Equal(TimeSpan.FromMilliseconds(500), _factory.T1);
    }

    [Fact]
    public void T2_Default_Returns4Seconds()
    {
        Assert.Equal(TimeSpan.FromSeconds(4), _factory.T2);
    }

    [Fact]
    public void T4_Default_Returns5Seconds()
    {
        Assert.Equal(TimeSpan.FromSeconds(5), _factory.T4);
    }

    [Fact]
    public void TimerA_Initial_ReturnsT1()
    {
        var interval = _factory.TimerA(TimeSpan.Zero);
        Assert.Equal(_factory.T1, interval);
    }

    [Fact]
    public void TimerA_Progressive_ReturnsAddedT1()
    {
        var first = _factory.TimerA(TimeSpan.FromMilliseconds(500));
        var second = _factory.TimerA(first);
        
        Assert.Equal(TimeSpan.FromMilliseconds(1500), second);
    }

    [Fact]
    public void TimerB_Returns64T1()
    {
        var expected = TimeSpan.FromMilliseconds(500 * 64);
        Assert.Equal(expected, _factory.TimerB);
    }

    [Fact]
    public void TimerD_Udp_Returns32Seconds()
    {
        var factory = new SipTimerFactory(isUdpTransport: true);
        Assert.Equal(TimeSpan.FromSeconds(32), factory.TimerD);
    }

    [Fact]
    public void TimerD_Tcp_ReturnsZero()
    {
        var factory = new SipTimerFactory(isUdpTransport: false);
        Assert.Equal(TimeSpan.Zero, factory.TimerD);
    }

    [Fact]
    public void TimerE_Initial_ReturnsT1()
    {
        var interval = _factory.TimerE(TimeSpan.Zero);
        Assert.Equal(_factory.T1, interval);
    }

    [Fact]
    public void TimerE_Progressive_AddsT1UntilT2()
    {
        var atT2 = _factory.TimerE(TimeSpan.FromSeconds(3.5));
        Assert.Equal(TimeSpan.FromSeconds(4), atT2);
    }

    [Fact]
    public void TimerF_Returns64T1()
    {
        var expected = TimeSpan.FromMilliseconds(500 * 64);
        Assert.Equal(expected, _factory.TimerF);
    }

    [Fact]
    public void TimerG_Initial_ReturnsT1()
    {
        var interval = _factory.TimerG(TimeSpan.Zero);
        Assert.Equal(_factory.T1, interval);
    }

    [Fact]
    public void TimerH_Returns64T1()
    {
        var expected = TimeSpan.FromMilliseconds(500 * 64);
        Assert.Equal(expected, _factory.TimerH);
    }

    [Fact]
    public void TimerI_Udp_ReturnsT4()
    {
        var factory = new SipTimerFactory(isUdpTransport: true);
        Assert.Equal(factory.T4, factory.TimerI);
    }

    [Fact]
    public void TimerI_Tcp_ReturnsZero()
    {
        var factory = new SipTimerFactory(isUdpTransport: false);
        Assert.Equal(TimeSpan.Zero, factory.TimerI);
    }

    [Fact]
    public void TimerJ_Udp_Returns32Seconds()
    {
        var factory = new SipTimerFactory(isUdpTransport: true);
        Assert.Equal(TimeSpan.FromSeconds(32), factory.TimerJ);
    }

    [Fact]
    public void TimerJ_Tcp_ReturnsZero()
    {
        var factory = new SipTimerFactory(isUdpTransport: false);
        Assert.Equal(TimeSpan.Zero, factory.TimerJ);
    }

    [Fact]
    public void TimerK_Udp_ReturnsT4()
    {
        var factory = new SipTimerFactory(isUdpTransport: true);
        Assert.Equal(factory.T4, factory.TimerK);
    }

    [Fact]
    public void TimerK_Tcp_ReturnsZero()
    {
        var factory = new SipTimerFactory(isUdpTransport: false);
        Assert.Equal(TimeSpan.Zero, factory.TimerK);
    }

    [Fact]
    public void Create_ReturnsSystemTimer()
    {
        var timer = _factory.Create();
        Assert.NotNull(timer);
        Assert.False(timer.IsRunning);
    }

    [Fact]
    public void CustomT1_UsesProvidedValue()
    {
        var factory = new SipTimerFactory(t1: TimeSpan.FromMilliseconds(250));
        Assert.Equal(TimeSpan.FromMilliseconds(250), factory.T1);
    }
}
