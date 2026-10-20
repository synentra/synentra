using FluentValidation;

namespace Synentra.Application.Features.Approvals.Queries.GetApproval;

public sealed class GetApprovalQueryValidator : AbstractValidator<GetApprovalQuery>
{
    public GetApprovalQueryValidator()
    {
        RuleFor(x => x.ApprovalId).NotEmpty();
    }
}
