using FluentValidation;

namespace Synentra.Application.Features.Executions.Queries.GetExecution;

public sealed class GetExecutionQueryValidator : AbstractValidator<GetExecutionQuery>
{
    public GetExecutionQueryValidator()
    {
        RuleFor(x => x.ExecutionId).NotEmpty();
    }
}
