namespace Synentra.Domain.Common.Diagnostics;

public enum DiagnosticKind : byte
{
    System = 00,
    Infrastructure = 01,
    Serialization = 02,
    Unauthorized = 03,
    Validation = 04,
    NotFound = 05,
    Conflict = 06,
    Forbidden = 07,
    Authentication = 08
}