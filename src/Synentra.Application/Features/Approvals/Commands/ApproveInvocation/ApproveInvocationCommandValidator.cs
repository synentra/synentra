using FluentValidation;

namespace Synentra.Application.Features.Approvals.Commands.ApproveInvocation;

public sealed class ApproveInvocationCommandValidator : AbstractValidator<ApproveInvocationCommand>
{
    public ApproveInvocationCommandValidator()
    {
        RuleFor(x => x.ApprovalId).NotEmpty();
        RuleFor(x => x.Reviewer).NotNull();
    }
}
