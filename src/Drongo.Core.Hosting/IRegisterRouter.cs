namespace Drongo.Core.Hosting;

public interface IRegisterRouter
{
    Task RouteAsync(RegisterContext context);
}
