namespace Synentra.Application.Features.Invocations.Normalization;

public sealed record NormalizationInput(
    string OperationId, 
    string BindingId, 
    string? TaskGrantId, 
    IReadOnlyDictionary<string, object> Arguments, 
    IReadOnlyDictionary<string, string> Context, 
    IReadOnlyDictionary<string, string> TransportDetails, 
    string? IdempotencyKey, 
    string? DeclaredPurpose);
