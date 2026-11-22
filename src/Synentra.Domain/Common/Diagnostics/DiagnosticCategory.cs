namespace Synentra.Domain.Common.Diagnostics;

public enum DiagnosticCategory : byte
{
    Validation     = 0,
    Authentication = 1,
    Forbidden      = 2,
    NotFound       = 3,
    Conflict       = 4,
    Infrastructure = 5,
}