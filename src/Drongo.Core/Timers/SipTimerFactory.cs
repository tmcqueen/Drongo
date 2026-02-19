using System.Timers;

namespace Drongo.Core.Timers;

public sealed class SipTimerFactory : ITimerFactory
{
    private readonly TimeSpan _t1;
    private readonly TimeSpan _t2;
    private readonly TimeSpan _t4;
    private readonly TimeSpan _timerD;
    private readonly TimeSpan _timerH;
    private readonly TimeSpan _timerI;
    private readonly TimeSpan _timerJ;
    private readonly TimeSpan _timerK;

    public SipTimerFactory(
        TimeSpan? t1 = null,
        TimeSpan? t2 = null,
        TimeSpan? t4 = null,
        bool isUdpTransport = true)
    {
        _t1 = t1 ?? TimeSpan.FromMilliseconds(500);
        _t2 = t2 ?? TimeSpan.FromSeconds(4);
        _t4 = t4 ?? TimeSpan.FromSeconds(5);

        _timerD = isUdpTransport ? TimeSpan.FromSeconds(32) : TimeSpan.Zero;
        _timerH = TimeSpan.FromSeconds(32);
        _timerI = isUdpTransport ? _t4 : TimeSpan.Zero;
        _timerJ = isUdpTransport ? TimeSpan.FromSeconds(32) : TimeSpan.Zero;
        _timerK = isUdpTransport ? _t4 : TimeSpan.Zero;
    }

    public TimeSpan T1 => _t1;
    public TimeSpan T2 => _t2;
    public TimeSpan T4 => _t4;

    public TimeSpan TimerA(TimeSpan currentInterval)
    {
        return currentInterval + _t1;
    }

    public TimeSpan TimerB => TimeSpan.FromTicks(_t1.Ticks * 64);

    public TimeSpan TimerD => _timerD;

    public TimeSpan TimerE(TimeSpan currentInterval)
    {
        return currentInterval + _t1;
    }

    public TimeSpan TimerF => TimeSpan.FromTicks(_t1.Ticks * 64);

    public TimeSpan TimerG(TimeSpan currentInterval)
    {
        return currentInterval + _t1;
    }

    public TimeSpan TimerH => _timerH;

    public TimeSpan TimerI => _timerI;

    public TimeSpan TimerJ => _timerJ;

    public TimeSpan TimerK => _timerK;

    public ITimer Create() => new SystemTimer();
}

public sealed class SystemTimer : ITimer
{
    private readonly System.Timers.Timer _timer;
    private Action? _callback;

    public bool IsRunning => _timer.Enabled;

    public SystemTimer()
    {
        _timer = new System.Timers.Timer();
        _timer.Elapsed += OnElapsed;
    }

    private void OnElapsed(object? sender, ElapsedEventArgs e)
    {
        _callback?.Invoke();
    }

    public void Start(TimeSpan dueTime, Action callback)
    {
        _callback = callback;
        _timer.Interval = dueTime.TotalMilliseconds;
        _timer.AutoReset = false;
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
        _callback = null;
    }

    public void Change(TimeSpan dueTime)
    {
        _timer.Interval = dueTime.TotalMilliseconds;
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Elapsed -= OnElapsed;
        _timer.Dispose();
    }
}
