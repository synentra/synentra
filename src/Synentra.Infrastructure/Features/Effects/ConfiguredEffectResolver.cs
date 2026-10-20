using System.Text.Json;
using Synentra.Application.Features.Effects.Contracts;
using Synentra.Domain.Invocations;
using Synentra.Domain.Resources.ValueObjects;
using Synentra.Domain.Effects.ValueObjects;
using Synentra.Infrastructure.Features.Policies;
using Synentra.Domain.Operations;

namespace Synentra.Infrastructure.Features.Effects;

public sealed class ConfiguredEffectResolver : IEffectResolver
{
    public string ResolverId => "configured-effect";
    public string Version => "1.0.0";

    public Task<EffectPlan> ResolveAsync(
        ToolInvocation invocation, 
        IReadOnlyList<ResourceDescriptor> resources, 
        EffectRegistration registration,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var c = registration.Configuration;
        string Required(string key) => c.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) 
            ? value 
            : throw new InvalidOperationException("Effect registration field missing: " + key);

        var measure = Required("measure");
        var unit = Required("unit");
        var identity = Required("budgetIdentity");

        if (!int.TryParse(Required("scale"), out var scale) || scale < 0)
            throw new InvalidOperationException("Invalid effect scale.");

        long amount;
        if (c.TryGetValue("upperBound", out var bound))
        {
            if (!long.TryParse(bound, out amount) || amount < 0)
                throw new InvalidOperationException("Invalid upper bound.");
        }
        else
        {
            if (!JsonPointer.TryRead(JsonSerializer.SerializeToElement(invocation.Arguments), Required("amountPointer"), out var value) || !value.TryGetInt64(out amount) || amount < 0)
                throw new InvalidOperationException("Effect amount must be a nonnegative integer.");

            if (!long.TryParse(Required("maximumAmount"), out var max) || amount > max)
                throw new InvalidOperationException("Effect amount exceeds registered maximum.");
        }

        return Task.FromResult(new EffectPlan(resources, invocation.IsMutation 
            ? Mutability.Mutation 
            : Mutability.ReadOnly, new[] { new EffectQuantity(measure, unit, scale, amount, true, identity) }, new Dictionary<string, string>(), registration.Version, 1));
    }
}
