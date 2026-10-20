namespace Synentra.Application.Features.Obligations.Contracts;

public sealed record ObligationResult(
    bool Satisfied, 
    string? ReasonCode = null, 
    string? RemediationRef = null, 
    IReadOnlyDictionary<string, string>? Outputs = null)
{
    public static ObligationResult Ok(IReadOnlyDictionary<string, string>? outputs = null) => new(true, null, null, outputs);
    public static ObligationResult Fail(string reasonCode, string? remediation = null) => new(false, reasonCode, remediation);
}
