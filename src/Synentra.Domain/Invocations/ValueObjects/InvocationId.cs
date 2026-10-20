namespace Synentra.Domain.Invocations.ValueObjects;

public readonly record struct InvocationId(Guid Value)
{
    public static InvocationId New() => new(Guid.NewGuid());
    public static InvocationId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();
}
