namespace Synentra.Domain.Governance.Risk.ValueObjects;

public sealed record RiskAssessment
{
    public RiskScore Score { get; }
    public IReadOnlyList<string> MissingFactors { get; }
    public string CalculatorVersion { get; }
    public IReadOnlyList<string> Explanations { get; }
    public IReadOnlyList<RiskFactorResult> Factors { get; }

    public RiskAssessment(
        RiskScore score, 
        IReadOnlyList<string> missingFactors, 
        string calculatorVersion,
        IReadOnlyList<string> explanations,
        IReadOnlyList<RiskFactorResult>? factors = null)
    {
        Score = score;
        MissingFactors = missingFactors;
        CalculatorVersion = calculatorVersion;
        Explanations = explanations;
        Factors = factors ?? Array.Empty<RiskFactorResult>();
    }
}
