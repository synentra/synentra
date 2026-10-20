using FluentValidation;

namespace Synentra.Application.Features.TaskGrants.Queries.ListActiveTaskGrants;

public sealed class ListActiveTaskGrantsQueryValidator : AbstractValidator<ListActiveTaskGrantsQuery>
{
    public ListActiveTaskGrantsQueryValidator()
    {
        RuleFor(x => x.AgentId).NotEmpty();
        RuleFor(x => x.SecurityScope).NotEmpty();
    }
}
