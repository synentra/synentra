namespace Synentra.Diagnostics;

public sealed record Diagnostic
{
    public string Code { get; }
    public string Message { get; }
    public DiagnosticCategory Category { get; }
    public ushort Sequence { get; }

    private Diagnostic(
        string code,
        string message,
        DiagnosticCategory category,
        ushort sequence)
    {
        Code = code;
        Message = message;
        Category = category;
        Sequence = sequence;
    }

    public static Diagnostic Define(
        string module,
        ushort sequence,
        string message,
        DiagnosticCategory category = DiagnosticCategory.Validation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new(DiagnosticCode.Build(module, sequence), message, category, sequence);
    }

    public override string ToString()
    {
        return $"{Category.ToString().ToLowerInvariant()} {Code}: {Message}";
    }
}