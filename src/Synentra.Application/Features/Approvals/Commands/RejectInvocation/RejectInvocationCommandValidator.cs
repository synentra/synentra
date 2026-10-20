using FluentValidation;

namespace Synentra.Application.Features.Approvals.Commands.RejectInvocation;

public sealed class RejectInvocationCommandValidator : FluentValidation.AbstractValidator<RejectInvocationCommand>
{
    public RejectInvocationCommandValidator()
    {
        RuleFor(x => x.ApprovalId).NotEmpty();
        RuleFor(x => x.Reviewer).NotNull();
    }
}
