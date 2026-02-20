namespace Drongo.Core.Hosting;

public interface IApplicationLifetime
{
    ApplicationEvent ApplicationStarting { get; }
    ApplicationEvent ApplicationStopping { get; }
    ApplicationEvent ApplicationStarted { get; }
}

public delegate Task ApplicationLifecycleCallback(ApplicationContext context);
