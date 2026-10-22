using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Capabilities.Queries.GetCapabilities;
using Synentra.Domain.Common.Results;

namespace Synentra.Application.Features.Capabilities;

public static class CapabilityDispatcherExtensions
{
    public static async Task<Result<GetCapabilitiesResponse>> GetCapabilities(
        this IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        return await dispatcher.Dispatch(new GetCapabilitiesQuery(), cancellationToken);
    }
}
