using FluentValidation;

namespace Synentra.Application.Features.Approvals.Queries.ListPendingApprovals;

public sealed class ListPendingApprovalsQueryValidator : AbstractValidator<ListPendingApprovalsQuery>
{
    public ListPendingApprovalsQueryValidator()
    {
        RuleFor(x => x.SecurityScope).NotEmpty();
        RuleFor(x => x.PageRequest.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageRequest.PageSize).GreaterThan(0);
    }
}
