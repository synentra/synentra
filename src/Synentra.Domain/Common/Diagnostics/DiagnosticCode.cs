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
    public const string Issuer = "SYNENTRA";          // single source of truth
    public const int MinModuleLength = 3;
    public const int MaxModuleLength = 4;
    public const int SequenceDigits = 4;
    public const int MinSequence = 1;
    public const int MaxSequence = 9999;

    public static string Build(string module, ushort sequence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);

        if (!IsValidModule(module))
            throw new ArgumentException(
                $"Module must be {MinModuleLength}-{MaxModuleLength} characters, " +
                "start with an uppercase ASCII letter, and contain only uppercase ASCII letters or digits.",
                nameof(module));

        if (sequence is < MinSequence or > MaxSequence)
            throw new ArgumentOutOfRangeException(
                nameof(sequence),
                $"Sequence must be between {MinSequence} and {MaxSequence}.");

        return $"{Issuer}-{module}-{sequence:D4}";
    }

    public static bool IsValid(string? code) =>
        code is not null && DiagnosticCodeRegex().IsMatch(code);

    public static bool IsValidModule(string? module) =>
        module is { Length: >= MinModuleLength and <= MaxModuleLength }
        && char.IsAsciiLetterUpper(module[0])
        && module.All(c => char.IsAsciiLetterUpper(c) || char.IsAsciiDigit(c));

    public static string GetModule(string code) => Parse(code).Module;
    public static ushort GetSequence(string code) => Parse(code).Sequence;

    public static DiagnosticCodeParts Parse(string code)
    {
        if (!IsValid(code))
            throw new ArgumentException($"Invalid Synentra diagnostic code: '{code}'.", nameof(code));

        var match = DiagnosticCodeRegex().Match(code);

        return new DiagnosticCodeParts(
            Issuer: match.Groups["issuer"].Value,
            Module: match.Groups["module"].Value,
            Sequence: ushort.Parse(match.Groups["sequence"].Value, CultureInfo.InvariantCulture));
    }

    // issuer is hard-coded here; move to an instance-based regex if it must be configurable.
    [GeneratedRegex(
        @"\A(?<issuer>SYNENTRA)-(?<module>[A-Z][A-Z0-9]{2,10})-(?!0000)(?<sequence>[0-9]{4})\z",
        RegexOptions.CultureInvariant)]
    private static partial Regex DiagnosticCodeRegex();
}

public readonly record struct DiagnosticCodeParts(string Issuer, string Module, ushort Sequence);