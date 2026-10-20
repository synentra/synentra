namespace Synentra.Application.Features.Operations.Contracts;

public sealed record OperationDto(
    string Id, 
    string Version, 
    string Family, 
    string Action, 
    string Mutability,
    string ExecutionModel, 
    bool TaskGrantRequired, 
    IReadOnlyList<string> PolicyRefs, 
    IReadOnlyList<string> BindingIds);
