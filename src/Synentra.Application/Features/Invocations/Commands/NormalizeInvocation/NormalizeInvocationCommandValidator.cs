using FluentValidation;

namespace Synentra.Application.Features.Invocations.Commands.NormalizeInvocation;

public sealed class NormalizeInvocationCommandValidator : AbstractValidator<NormalizeInvocationCommand>
{
    public NormalizeInvocationCommandValidator()
    {
        RuleFor(x => x.Input).NotNull();
        RuleFor(x => x.Principal).NotNull();
    }
}
