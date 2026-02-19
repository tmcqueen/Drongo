namespace Drongo.Core.Timers;

public interface ITimer : IDisposable
{
    bool IsRunning { get; }
    void Start(TimeSpan dueTime, Action callback);
    void Stop();
    void Change(TimeSpan dueTime);
}

public interface ITimerFactory
{
    ITimer Create();
    TimeSpan T1 { get; }
    TimeSpan T2 { get; }
    TimeSpan T4 { get; }
    TimeSpan TimerA(TimeSpan currentInterval);
    TimeSpan TimerB { get; }
    TimeSpan TimerD { get; }
    TimeSpan TimerE(TimeSpan currentInterval);
    TimeSpan TimerF { get; }
    TimeSpan TimerG(TimeSpan currentInterval);
    TimeSpan TimerH { get; }
    TimeSpan TimerI { get; }
    TimeSpan TimerJ { get; }
    TimeSpan TimerK { get; }
}
