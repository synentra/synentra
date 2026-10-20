using System.Globalization;
using System.Text.RegularExpressions;

namespace Synentra.Domain.Common.Diagnostics;

/// <summary>
/// Provides standardized, stable diagnostic identifiers.
///
/// Format: SYN-{MODULE}-{SEQUENCE}
/// Example: SYN-APR-0001
///
/// Diagnostic identifiers are public contracts and must not
/// be changed or reused once published.
/// </summary>
public static partial class DiagnosticCode
{
    public const string Prefix = "SYN";
    public const int ModuleLength = 3;
    public const int SequenceLength = 4;
    public const int MinSequence = 1;
    public const int MaxSequence = 9999;

    /// <summary>
    /// Builds a diagnostic code from a module and sequence.
    /// </summary>
    public static string Build(
        string module,
        ushort sequence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);

        if (!IsValidModule(module))
        {
            throw new ArgumentException(
                $"Module must contain exactly {ModuleLength} uppercase ASCII letters.",
                nameof(module));
        }

        if (sequence is < MinSequence or > MaxSequence)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sequence),
                $"Sequence must be between {MinSequence} and {MaxSequence}.");
        }

        return $"{Prefix}-{module}-{sequence:D4}";
    }

    /// <summary>
    /// Checks whether the diagnostic code follows
    /// the standard Synentra format.
    /// </summary>
    public static bool IsValid(string? code)
    {
        return code is not null &&
               DiagnosticCodeRegex().IsMatch(code);
    }

    /// <summary>
    /// Checks whether a module identifier is valid.
    /// </summary>
    public static bool IsValidModule(string? module)
    {
        return module is
        {
            Length: ModuleLength
        } &&
        module.All(c => c is >= 'A' and <= 'Z');
    }

    /// <summary>
    /// Extracts the module from a valid diagnostic code.
    /// </summary>
    public static string GetModule(string code)
    {
        EnsureValid(code);

        return code.Substring(Prefix.Length + 1, ModuleLength);
    }

    /// <summary>
    /// Extracts the numeric sequence from a valid diagnostic code.
    /// </summary>
    public static ushort GetSequence(string code)
    {
        EnsureValid(code);

        return ushort.Parse(
            code.AsSpan(code.Length - SequenceLength),
            CultureInfo.InvariantCulture);
    }

    private static void EnsureValid(string? code)
    {
        if (!IsValid(code))
        {
            throw new ArgumentException(
                $"Invalid Synentra diagnostic code: '{code}'.",
                nameof(code));
        }
    }

    [GeneratedRegex(
        @"\ASYN-[A-Z]{3}-(?!0000)[0-9]{4}\z",
        RegexOptions.CultureInvariant)]
    private static partial Regex DiagnosticCodeRegex();
}