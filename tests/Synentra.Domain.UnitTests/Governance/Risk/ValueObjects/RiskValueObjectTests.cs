using FluentAssertions;
using Synentra.Diagnostics.Catalog;
using Synentra.Diagnostics.Exceptions;
using Synentra.Domain.Governance.Risk;
using Synentra.Domain.Governance.Risk.ValueObjects;

namespace Synentra.Domain.Tests;

public sealed class RiskValueObjectTests
{
    [Theory]
    [InlineData(0d, 0d)]
    [InlineData(0.5d, 0.75d)]
    [InlineData(1d, 1d)]
    public void RiskScore_ShouldAcceptInclusiveRange(double score, double coverage)
    {
        var risk = new RiskScore(score, coverage);
        risk.Value.Should().Be(score);
        risk.Coverage.Should().Be(coverage);
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(1.01d)]
    public void RiskScore_ShouldRejectOutOfRangeScore(double score)
        => FluentActions.Invoking(() => new RiskScore(score, 0.5))
            .Should().Throw<SynentraDomainException>()
            .Which.Diagnostic.Should().Be(RiskDiagnostics.RiskScoreOutOfRange);

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(1.01d)]
    public void RiskScore_ShouldRejectOutOfRangeCoverage(double coverage)
        => FluentActions.Invoking(() => new RiskScore(0.5, coverage))
            .Should().Throw<SynentraDomainException>()
            .Which.Diagnostic.Should().Be(RiskDiagnostics.CoverageOutOfRange);

    [Fact]
    public void RiskAssessment_ShouldPreserveInputsAndDefaultFactors()
    {
        var score = new RiskScore(0.3, 0.8);
        var factor = new RiskFactorResult("privilege", 0.4, 0.6, "limited scope", RiskFactorStatus.Applied);
        var assessment = new RiskAssessment(score, ["network"], "calc-v1", ["reason" ]);
        var withFactors = new RiskAssessment(score, [], "calc-v2", [], [factor]);

        assessment.Score.Should().Be(score);
        assessment.MissingFactors.Should().Equal("network");
        assessment.CalculatorVersion.Should().Be("calc-v1");
        assessment.Explanations.Should().Equal("reason");
        assessment.Factors.Should().BeEmpty();
        withFactors.Factors.Should().ContainSingle().Which.Should().Be(factor);
        factor.Name.Should().Be("privilege");
        factor.Score.Should().Be(0.4);
        factor.Weight.Should().Be(0.6);
        factor.Explanation.Should().Be("limited scope");
        factor.Status.Should().Be(RiskFactorStatus.Applied);
    }
}
