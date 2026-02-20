using Microsoft.Extensions.DependencyInjection;

namespace Drongo.Core.Hosting;

public sealed record ApplicationContext
{
    public IServiceProvider Services { get; init; } = null!;
    public IReadOnlyList<EndpointInfo> Endpoints { get; init; } = Array.Empty<EndpointInfo>();
}
