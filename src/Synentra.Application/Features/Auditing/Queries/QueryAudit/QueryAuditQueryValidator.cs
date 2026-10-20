using FluentValidation;

namespace Synentra.Application.Features.Auditing.Queries.QueryAudit;
public sealed class QueryAuditQueryValidator : FluentValidation.AbstractValidator<QueryAuditQuery>
{
    public QueryAuditQueryValidator()
    {
        RuleFor(x => x.Filter).NotNull();
    }
}
