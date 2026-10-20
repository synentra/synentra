using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Plugins;

namespace Synentra.Application.Features.Capabilities.Queries.GetCapabilities;

public sealed class GetCapabilitiesQueryHandler(PluginCatalog catalog) : IActionHandler<GetCapabilitiesQuery, GetCapabilitiesResponse>
{
    public Task<GetCapabilitiesResponse> Handle(GetCapabilitiesQuery request, CancellationToken cancellationToken = default)
    {
        var adapters = catalog.All
            .Where(x => x.Family == PluginFamily.TransportAdapter)
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .Select(x => new GetCapabilityDto(
                x.Id,
                x.Version,
                x.Capabilities.Where(c => c is "request_response" or "asynchronous" or "stream" or "batch").Order().ToArray(),
                new[] { x.ContractVersion },
                x.Capabilities.Order().ToArray(),
                Array.Empty<string>()))
            .ToArray();

        return Task.FromResult(new GetCapabilitiesResponse(adapters));
    }
}
