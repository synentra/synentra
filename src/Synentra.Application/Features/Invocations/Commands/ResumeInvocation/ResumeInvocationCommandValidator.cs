using FluentValidation;

namespace Synentra.Application.Features.Invocations.Commands.ResumeInvocation;

public sealed class ResumeInvocationCommandValidator : AbstractValidator<ResumeInvocationCommand>
{
    public ResumeInvocationCommandValidator()
    {
        RuleFor(x => x.InvocationId).NotEmpty();
        RuleFor(x => x.Caller).NotNull();
    }
}
