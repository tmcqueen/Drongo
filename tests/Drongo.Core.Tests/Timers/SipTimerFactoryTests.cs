using Drongo.Core.SIP.Timers;
using Xunit;
using Shouldly;

namespace Drongo.Core.Tests.Timers;

public class SipTimerFactoryTests
{
    private readonly SipTimerFactory _factory = new();

    [Fact]
    public void T1_Default_Returns500Ms()
    {
        _factory.T1.ShouldBe(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void T2_Default_Returns4Seconds()
    {
        _factory.T2.ShouldBe(TimeSpan.FromSeconds(4));
    }

    [Fact]
    public void T4_Default_Returns5Seconds()
    {
        _factory.T4.ShouldBe(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void TimerA_Initial_ReturnsT1()
    {
        var interval = _factory.TimerA(TimeSpan.Zero);
        interval.ShouldBe(_factory.T1);
    }

    [Fact]
    public void TimerA_Progressive_ReturnsAddedT1()
    {
        var first = _factory.TimerA(TimeSpan.FromMilliseconds(500));
        var second = _factory.TimerA(first);
        
        second.ShouldBe(TimeSpan.FromMilliseconds(1500));
    }

    [Fact]
    public void TimerB_Returns64T1()
    {
        var expected = TimeSpan.FromMilliseconds(500 * 64);
        _factory.TimerB.ShouldBe(expected);
    }

    [Fact]
    public void TimerD_Udp_Returns32Seconds()
    {
        var factory = new SipTimerFactory(isUdpTransport: true);
        factory.TimerD.ShouldBe(TimeSpan.FromSeconds(32));
    }

    [Fact]
    public void TimerD_Tcp_ReturnsZero()
    {
        var factory = new SipTimerFactory(isUdpTransport: false);
        factory.TimerD.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void TimerE_Initial_ReturnsT1()
    {
        var interval = _factory.TimerE(TimeSpan.Zero);
        interval.ShouldBe(_factory.T1);
    }

    [Fact]
    public void TimerE_Progressive_AddsT1UntilT2()
    {
        var atT2 = _factory.TimerE(TimeSpan.FromSeconds(3.5));
        atT2.ShouldBe(TimeSpan.FromSeconds(4));
    }

    [Fact]
    public void TimerF_Returns64T1()
    {
        var expected = TimeSpan.FromMilliseconds(500 * 64);
        _factory.TimerF.ShouldBe(expected);
    }

    [Fact]
    public void TimerG_Initial_ReturnsT1()
    {
        var interval = _factory.TimerG(TimeSpan.Zero);
        interval.ShouldBe(_factory.T1);
    }

    [Fact]
    public void TimerH_Returns64T1()
    {
        var expected = TimeSpan.FromMilliseconds(500 * 64);
        _factory.TimerH.ShouldBe(expected);
    }

    [Fact]
    public void TimerI_Udp_ReturnsT4()
    {
        var factory = new SipTimerFactory(isUdpTransport: true);
        factory.TimerI.ShouldBe(factory.T4);
    }

    [Fact]
    public void TimerI_Tcp_ReturnsZero()
    {
        var factory = new SipTimerFactory(isUdpTransport: false);
        factory.TimerI.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void TimerJ_Udp_Returns32Seconds()
    {
        var factory = new SipTimerFactory(isUdpTransport: true);
        factory.TimerJ.ShouldBe(TimeSpan.FromSeconds(32));
    }

    [Fact]
    public void TimerJ_Tcp_ReturnsZero()
    {
        var factory = new SipTimerFactory(isUdpTransport: false);
        factory.TimerJ.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void TimerK_Udp_ReturnsT4()
    {
        var factory = new SipTimerFactory(isUdpTransport: true);
        factory.TimerK.ShouldBe(factory.T4);
    }

    [Fact]
    public void TimerK_Tcp_ReturnsZero()
    {
        var factory = new SipTimerFactory(isUdpTransport: false);
        factory.TimerK.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void Create_ReturnsSystemTimer()
    {
        var timer = _factory.Create();
        timer.ShouldNotBeNull();
        timer.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public void CustomT1_UsesProvidedValue()
    {
        var factory = new SipTimerFactory(t1: TimeSpan.FromMilliseconds(250));
        factory.T1.ShouldBe(TimeSpan.FromMilliseconds(250));
    }
}
