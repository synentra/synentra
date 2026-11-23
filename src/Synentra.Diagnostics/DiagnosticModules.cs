namespace Synentra.Diagnostics;

/// <summary>
/// Canonical module identifiers used in diagnostic codes
/// (format: <c>SYNENTRA-{MODULE}-{SEQUENCE}</c>, e.g. <c>SYNENTRA-AUTH-3002</c>).
/// Modules are 3–5 uppercase ASCII letters. Once published, a module code
/// must not be changed or reused.
/// </summary>
public static class DiagnosticModules
{
    public const string Adapters = "ADP";
    public const string Agents = "AGT";
    public const string Approvals = "APR";
    public const string Authentication = "AUTH";
    public const string BindingDigest = "BDG";
    public const string Credentials = "CRD";
    public const string EffectMeasure = "EFM";
    public const string Executions = "EXE";
    public const string Governance = "GOV";
    public const string Idempotency = "IDM";
    public const string Infrastructure = "INF";
    public const string Intent = "INT";
    public const string Invocations = "INV";
    public const string Manifest = "MFT";
    public const string Mcp = "MCP";
    public const string Operation = "OPR";
    public const string Persistence = "PER";
    public const string Plugins = "PLG";
    public const string Policies = "POL";
    public const string Quarantine = "QRT";
    public const string ResourceDescriptor = "RSD";
    public const string Risk = "RSK";
    public const string Secrets = "SEC";
    public const string TaskGrants = "TGR";
    public const string Trust = "TRS";
}