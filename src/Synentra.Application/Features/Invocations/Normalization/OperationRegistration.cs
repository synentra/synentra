using Synentra.Domain.Operations;

namespace Synentra.Application.Features.Invocations.Normalization;

public sealed record OperationRegistration(
    string OperationId, 
    string Version, 
    string Family, 
    string Action, 
    Mutability Mutability, 
    ExecutionModel ExecutionModel, 
    bool TaskGrantRequired, 
    bool HasEffectRegistration, 
    IReadOnlyList<string> PolicyRefs);
