using Synentra.Application.Features.Risk.Contracts;
using Synentra.Domain.Governance.Risk;
using Synentra.Domain.Governance.Risk.ValueObjects;

namespace Synentra.Infrastructure.Features.Risk;

public sealed class DefaultRiskEngine(IEnumerable<IRiskFactorPlugin> factors) : IRiskEngine
{
    public string CalculatorId => "weighted-risk";
    public string Version => "1.0.0";

    public async Task<RiskAssessment> EvaluateAsync(RiskInput input, CancellationToken ct)
    {
        var results = new List<RiskFactorResult>();
        var missing = new List<string>();
        var plugins = factors.ToArray();

        foreach (var factor in plugins)
        {
            ct.ThrowIfCancellationRequested();
            if (!factor.SupportedModalities.HasFlag(input.Invocation.Modality))
            {
                missing.Add(factor.Name);
                continue;
            }

            try
            {
                var result = await factor.EvaluateAsync(input, ct);
                if (!double.IsFinite(result.Score) || !double.IsFinite(result.Weight) || result.Score is < 0 or > 1 || result.Weight <= 0 || result.Status != RiskFactorStatus.Applied)
                    missing.Add(factor.Name);
                else
                    results.Add(result);
            }
            catch (OperationCanceledException)when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                missing.Add(factor.Name);
            }
        }

        var score = results.Count == 0 ? 1 : results.Sum(x => x.Score * x.Weight) / results.Sum(x => x.Weight);
        var coverage = plugins.Length == 0 ? 0 : (double)results.Count / plugins.Length;
        return new(new RiskScore(score, coverage), missing, Version, results.Select(x => x.Explanation).ToArray(), results);
    }
}
