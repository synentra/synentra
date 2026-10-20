namespace Synentra.Domain.Common.Diagnostics;

public sealed record Diagnostic
{
    public string Code { get; }
    public string Message { get; }
    public DiagnosticKind Kind { get; }
    public ushort Sequence { get; }

    private Diagnostic(
        string code,
        string message,
        DiagnosticKind kind,
        ushort sequence)
    {
        Code = code;
        Message = message;
        Kind = kind;
        Sequence = sequence;
    }

    public static Diagnostic Define(
        string module,
        ushort sequence,
        DiagnosticKind kind,
        string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        var code = DiagnosticCode.Build(module, sequence);

        return new Diagnostic(
            code,
            message,
            kind,
            sequence);
    }

    public override string ToString()
    {
        return $"{Kind.ToString().ToLowerInvariant()} {Code}: {Message}";
    }
}