using FluentValidation;

namespace Synentra.Application.Features.Approvals.Commands.RevokeApproval;

public sealed class RevokeApprovalCommandValidator : AbstractValidator<RevokeApprovalCommand>
{
    public RevokeApprovalCommandValidator()
    {
        RuleFor(x => x.ApprovalId).NotEmpty();
        RuleFor(x => x.By).NotNull();
    }
}
