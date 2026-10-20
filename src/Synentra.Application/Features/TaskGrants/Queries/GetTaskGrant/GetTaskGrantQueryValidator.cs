using FluentValidation;

namespace Synentra.Application.Features.TaskGrants.Queries.GetTaskGrant;

public sealed class GetTaskGrantQueryValidator : AbstractValidator<GetTaskGrantQuery>
{
    public GetTaskGrantQueryValidator()
    {
        RuleFor(x => x.GrantId).NotEmpty();
    }
}
