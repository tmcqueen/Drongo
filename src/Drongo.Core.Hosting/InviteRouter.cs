namespace Drongo.Core.Hosting;

public sealed class InviteRouter : IInviteRouter
{
    private readonly List<Func<InviteContext, Task>> _handlers;

    public InviteRouter(List<Func<InviteContext, Task>> handlers)
    {
        _handlers = handlers;
    }

    public async Task RouteAsync(InviteContext context)
    {
        foreach (var handler in _handlers)
        {
            await handler(context);
        }
    }
}
