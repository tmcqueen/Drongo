namespace Drongo.Routing.Routes;

public abstract class RouteBase : IRoute
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string OrganizationId { get; init; } = string.Empty;
    public string Protocol { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string SenderAddress { get; init; } = string.Empty;
    public string ReceiverAddress { get; init; } = string.Empty;
}
