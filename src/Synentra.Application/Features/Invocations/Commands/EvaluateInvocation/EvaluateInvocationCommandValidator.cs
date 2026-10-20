using FluentValidation;

namespace Synentra.Application.Features.Invocations.Commands.EvaluateInvocation;

public sealed class EvaluateInvocationCommandValidator : AbstractValidator<EvaluateInvocationCommand>
{
    public EvaluateInvocationCommandValidator()
    {
        RuleFor(x => x.Invocation).NotNull();
    }
}
