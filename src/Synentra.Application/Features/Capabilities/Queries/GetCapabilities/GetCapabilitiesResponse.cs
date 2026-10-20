namespace Synentra.Application.Features.Capabilities.Queries.GetCapabilities;

public sealed record GetCapabilitiesResponse(IReadOnlyList<GetCapabilityDto> Adapters);

public sealed record GetCapabilityDto(
    string AdapterId,
    string Version,
    IReadOnlyList<string> SupportedExecutionModels,
    IReadOnlyList<string> SupportedSchemaVersions,
    IReadOnlyList<string> EnforcementCapabilities,
    IReadOnlyList<string> SupportedModalities);
