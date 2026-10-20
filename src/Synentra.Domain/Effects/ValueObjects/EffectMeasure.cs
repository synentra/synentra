using Synentra.Domain.Common.Errors;
using Synentra.Domain.Common.Exceptions;

namespace Synentra.Domain.Effects.ValueObjects;

public sealed record EffectMeasure
{
    public string Measure { get; }
    public string Unit { get; }
    public int Scale { get; }

    public EffectMeasure(string measure, string unit, int scale)
    {
        if (string.IsNullOrWhiteSpace(measure))
            throw new DomainException(DomainErrors.EffectMeasure.MeasureRequired);
        if (string.IsNullOrWhiteSpace(unit))
            throw new DomainException(DomainErrors.EffectMeasure.UnitRequired);
        if (scale < 0)
            throw new DomainException(DomainErrors.EffectMeasure.ScaleMustBeNonNegative);
        Measure = measure;
        Unit = unit;
        Scale = scale;
    }

    public bool IsCompatibleWith(EffectMeasure other) => Measure == other.Measure && Unit == other.Unit && Scale == other.Scale;
}
