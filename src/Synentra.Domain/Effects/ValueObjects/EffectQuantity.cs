using Synentra.Domain.Common.Errors;
using Synentra.Domain.Common.Exceptions;

namespace Synentra.Domain.Effects.ValueObjects;

public sealed record EffectQuantity
{
    public string Measure { get; }
    public string Unit { get; }
    public int Scale { get; }
    public long UpperBound { get; }
    public bool IsBounded { get; }
    public string? BudgetIdentity { get; }

    public EffectQuantity(
        string measure, string unit, 
        int scale, long upperBound, 
        bool isBounded, string? budgetIdentity = null)
    {
        if (upperBound < 0)
            throw new DomainException(DomainErrors.EffectMeasure.UpperBoundMustBeNonNegative);

        Measure = measure;
        Unit = unit;
        Scale = scale;
        UpperBound = upperBound;
        IsBounded = isBounded;
        BudgetIdentity = budgetIdentity;
    }

    public EffectMeasure AsMeasure() => new(Measure, Unit, Scale);
}
