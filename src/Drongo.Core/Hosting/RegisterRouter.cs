namespace Drongo.Core.Hosting;

public sealed class RegisterRouter : IRegisterRouter
{
    private readonly List<Func<RegisterContext, Task>> _handlers;

    public RegisterRouter(List<Func<RegisterContext, Task>> handlers)
    {
        _handlers = handlers;
    }

    public async Task RouteAsync(RegisterContext context)
    {
        foreach (var handler in _handlers)
        {
            await handler(context);
        }
    }
}
