namespace Drongo.Core.Hosting;

public sealed class ApplicationLifetime : IApplicationLifetime
{
    private readonly List<ApplicationLifecycleCallback> _startingCallbacks = new();
    private readonly List<ApplicationLifecycleCallback> _stoppingCallbacks = new();
    private readonly List<ApplicationLifecycleCallback> _startedCallbacks = new();

    public ApplicationEvent ApplicationStarting { get; }
    public ApplicationEvent ApplicationStopping { get; }
    public ApplicationEvent ApplicationStarted { get; }

    public ApplicationLifetime()
    {
        ApplicationStarting = new ApplicationEvent(_startingCallbacks);
        ApplicationStopping = new ApplicationEvent(_stoppingCallbacks);
        ApplicationStarted = new ApplicationEvent(_startedCallbacks);
    }

    internal async Task NotifyStartingAsync(ApplicationContext context)
    {
        foreach (var callback in _startingCallbacks)
        {
            await callback(context);
        }
    }

    internal async Task NotifyStoppingAsync(ApplicationContext context)
    {
        foreach (var callback in _stoppingCallbacks)
        {
            await callback(context);
        }
    }

    internal async Task NotifyStartedAsync(ApplicationContext context)
    {
        foreach (var callback in _startedCallbacks)
        {
            await callback(context);
        }
    }
}

public sealed class ApplicationEvent
{
    private readonly List<ApplicationLifecycleCallback> _callbacks;

    public ApplicationEvent(List<ApplicationLifecycleCallback> callbacks)
    {
        _callbacks = callbacks;
    }

    public void Register(ApplicationLifecycleCallback callback)
    {
        _callbacks.Add(callback);
    }
}
