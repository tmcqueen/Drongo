namespace Drongo.Core.Hosting;

public interface IInviteRouter
{
    Task RouteAsync(InviteContext context);
}
