using FluentValidation;

namespace Synentra.Application.Features.Invocations.Queries.GetInvocation;

public sealed class GetInvocationQueryValidator : AbstractValidator<GetInvocationQuery>
{
    public GetInvocationQueryValidator()
    {
        RuleFor(x => x.InvocationId).NotEmpty();
    }
}
