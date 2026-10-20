using FluentValidation;

namespace Synentra.Application.Features.Approvals.Commands.CreateApproval;

public sealed class CreateApprovalCommandValidator : AbstractValidator<CreateApprovalCommand>
{
    public CreateApprovalCommandValidator()
    {
        RuleFor(x => x.Invocation).NotNull();
        RuleFor(x => x.PolicyRevision).NotEmpty();
        RuleFor(x => x.ReviewObligationRevision).NotEmpty();
    }
}
