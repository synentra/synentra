using FluentAssertions;

namespace Synentra.Diagnostics.Tests;

public class DiagnosticCodeTests
{
    [Theory]
    [InlineData("APR", (ushort)1, "SYNENTRA-APR-0001")]
    [InlineData("INV", (ushort)42, "SYNENTRA-INV-0042")]
    [InlineData("XYZ", (ushort)9999, "SYNENTRA-XYZ-9999")]
    public void Build_ShouldCreateCanonicalCode(string module, ushort sequence, string expected)
    => DiagnosticCode.Build(module, sequence).Should().Be(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Build_ShouldRejectMissingModule(string? module)
        => FluentActions.Invoking(() => DiagnosticCode.Build(module!, 1))
            .Should().Throw<ArgumentException>();

    [Theory]
    [InlineData("abC")]
    [InlineData("AB")]
    [InlineData("ÅBC")]
    public void Build_ShouldRejectInvalidModule(string module)
        => FluentActions.Invoking(() => DiagnosticCode.Build(module, 1))
            .Should().Throw<ArgumentException>().WithParameterName("module");

    [Theory]
    [InlineData((ushort)0)]
    [InlineData((ushort)10000)]
    public void Build_ShouldRejectOutOfRangeSequence(ushort sequence)
        => FluentActions.Invoking(() => DiagnosticCode.Build("APR", sequence))
            .Should().Throw<ArgumentOutOfRangeException>().WithParameterName("sequence");

    [Theory]
    [InlineData("SYNENTRA-APR-0001", true)]
    [InlineData("SYNENTRA-XYZ-9999", true)]
    [InlineData("SYNENTRA-APR-0000", false)]
    [InlineData("synentra-APR-0001", false)]
    [InlineData("SYNENTRA-abC-0001", false)]
    [InlineData("SYNENTRA-APR-001", false)]
    [InlineData("SYNENTRA-APR-10000", false)]
    [InlineData("SYNENTRA-AP-0001", false)]
    [InlineData("SYNENTRA-APR-0001-extra", false)]
    [InlineData("xSYNENTRA-APR-0001", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValid_ShouldValidateCompleteFormat(string? code, bool expected)
        => DiagnosticCode.IsValid(code).Should().Be(expected);

    [Theory]
    [InlineData("ABC", true)]
    [InlineData("XYZ", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("AB", false)]
    [InlineData("ABCD", true)]
    [InlineData("AbC", false)]
    [InlineData("A1C", true)]
    [InlineData("ÅBC", false)]
    public void IsValidModule_ShouldAcceptOnlyThreeUppercaseAsciiLetters(string? module, bool expected)
        => DiagnosticCode.IsValidModule(module).Should().Be(expected);

    [Fact]
    public void GetModuleAndSequence_ShouldExtractSegments()
    {
        DiagnosticCode.GetModule("SYNENTRA-APR-0123").Should().Be("APR");
        DiagnosticCode.GetSequence("SYNENTRA-APR-0123").Should().Be((ushort)123);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("SYNENTRA-APR-0000")]
    [InlineData(null)]
    public void GetModuleAndSequence_ShouldRejectInvalidCode(string? code)
    {
        FluentActions.Invoking(() => DiagnosticCode.GetModule(code!))
            .Should().Throw<ArgumentException>().WithParameterName("code");
        FluentActions.Invoking(() => DiagnosticCode.GetSequence(code!))
            .Should().Throw<ArgumentException>().WithParameterName("code");
    }

    [Fact]
    public void Parse_returns_parts()
    {
        var parts = DiagnosticCode.Parse("SYNENTRA-AUTH-0042");
        parts.Issuer.Should().Be(DiagnosticCode.Issuer);
        parts.Module.Should().Be("AUTH");
        parts.Sequence.Should().Be(42);
    }

    [Theory]
    [InlineData("SYNENTRA-AUTH-0000")][InlineData("invalid")][InlineData(null)]
    public void Parse_rejects_invalid_codes(string? code)
        => FluentActions.Invoking(() => DiagnosticCode.Parse(code!)).Should().Throw<ArgumentException>();

    [Fact] public void GetModule_returns_module() => DiagnosticCode.GetModule("SYNENTRA-AUTH-0042").Should().Be("AUTH");
    [Fact] public void GetSequence_returns_sequence() => DiagnosticCode.GetSequence("SYNENTRA-AUTH-0042").Should().Be(42);
}
