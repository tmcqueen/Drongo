namespace Drongo.Routing;

public interface IRoute
{
    string Id { get; }

    string OrganizationId { get; }

    string Protocol { get; }

    string Location { get; }

    string SenderAddress { get; }

    string ReceiverAddress { get; }
}
