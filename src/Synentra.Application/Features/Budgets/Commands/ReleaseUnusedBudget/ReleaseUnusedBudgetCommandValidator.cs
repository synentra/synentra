using Synentra.Domain.Invocations.ValueObjects;
using FluentValidation;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Auditing.Contracts;
using Synentra.Application.Features.Budgets.Contracts;
using Synentra.Application.Features.Budgets;
using Synentra.Application.Features.Executions.Contracts;
using Unit = Synentra.Application.Common.Dispatching.Void;

namespace Synentra.Application.Features.Budgets.Commands.ReleaseUnusedBudget;
public sealed class ReleaseUnusedBudgetCommandValidator : FluentValidation.AbstractValidator<ReleaseUnusedBudgetCommand>
{
    public ReleaseUnusedBudgetCommandValidator()
    {
        RuleFor(x => x.ExecutionId).NotEmpty();
        RuleFor(x => x.InvocationId).NotEmpty();
        RuleFor(x => x.EvidenceRef).NotEmpty();
    }
}
