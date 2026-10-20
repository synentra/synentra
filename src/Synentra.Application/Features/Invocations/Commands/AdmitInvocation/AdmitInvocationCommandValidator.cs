using FluentValidation;

namespace Synentra.Application.Features.Invocations.Commands.AdmitInvocation;

public sealed class AdmitInvocationCommandValidator : AbstractValidator<AdmitInvocationCommand>
{
    public AdmitInvocationCommandValidator()
    {
        RuleFor(x => x.Decision).NotNull();
        RuleFor(x => x.Invocation).NotNull();
    }
}
