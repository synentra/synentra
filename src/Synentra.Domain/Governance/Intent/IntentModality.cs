namespace Synentra.Domain.Governance.Intent;

[Flags]
public enum IntentModality
{
    None = 0,
    Http = 1,
    Mcp = 2,
    FunctionCall = 4,
    Batch = 8,
    Stream = 16
}
