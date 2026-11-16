using FluentAssertions;
using Synentra.Domain.Common.Diagnostics;

namespace Synentra.Domain.Tests;

public sealed class DiagnosticCodeTests
{
    [Theory]
    [InlineData("APR", (ushort)1, "SYN-APR-0001")]
    [InlineData("INV", (ushort)42, "SYN-INV-0042")]
    [InlineData("XYZ", (ushort)9999, "SYN-XYZ-9999")]
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
    [InlineData("ABCD")]
    [InlineData("A1C")]
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
    [InlineData("SYN-APR-0001", true)]
    [InlineData("SYN-XYZ-9999", true)]
    [InlineData("SYN-APR-0000", false)]
    [InlineData("syn-APR-0001", false)]
    [InlineData("SYN-abC-0001", false)]
    [InlineData("SYN-APR-001", false)]
    [InlineData("SYN-APR-10000", false)]
    [InlineData("SYN-AP-0001", false)]
    [InlineData("SYN-APR-0001-extra", false)]
    [InlineData("xSYN-APR-0001", false)]
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
    [InlineData("ABCD", false)]
    [InlineData("AbC", false)]
    [InlineData("A1C", false)]
    [InlineData("ÅBC", false)]
    public void IsValidModule_ShouldAcceptOnlyThreeUppercaseAsciiLetters(string? module, bool expected)
        => DiagnosticCode.IsValidModule(module).Should().Be(expected);

    [Fact]
    public void GetModuleAndSequence_ShouldExtractSegments()
    {
        DiagnosticCode.GetModule("SYN-APR-0123").Should().Be("APR");
        DiagnosticCode.GetSequence("SYN-APR-0123").Should().Be((ushort)123);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("SYN-APR-0000")]
    [InlineData(null)]
    public void GetModuleAndSequence_ShouldRejectInvalidCode(string? code)
    {
        FluentActions.Invoking(() => DiagnosticCode.GetModule(code!))
            .Should().Throw<ArgumentException>().WithParameterName("code");
        FluentActions.Invoking(() => DiagnosticCode.GetSequence(code!))
            .Should().Throw<ArgumentException>().WithParameterName("code");
    }
}
