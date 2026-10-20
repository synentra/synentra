using Synentra.Domain.Common.Errors;
using Synentra.Domain.Common.Exceptions;

namespace Synentra.Domain.Effects.ValueObjects;

public sealed record Quantity
{
    public long Value { get; }
    public EffectMeasure Measure { get; }

    public Quantity(long value, EffectMeasure measure)
    {
        if (value < 0)
            throw new DomainException(DomainErrors.Quantity.NegativeQuantity);
        Value = value;
        Measure = measure;
    }

    public Quantity Add(Quantity other)
    {
        if (!Measure.IsCompatibleWith(other.Measure))
            throw new DomainException(DomainErrors.Quantity.IncompatibleUnits);
        checked
        {
            return new Quantity(Value + other.Value, Measure);
        }
    }
}
