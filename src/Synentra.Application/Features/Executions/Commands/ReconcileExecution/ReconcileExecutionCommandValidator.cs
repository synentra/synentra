using FluentValidation;

namespace Synentra.Application.Features.Executions.Commands.ReconcileExecution;
public sealed class ReconcileExecutionCommandValidator : AbstractValidator<ReconcileExecutionCommand>
{
    public ReconcileExecutionCommandValidator()
    {
        RuleFor(x => x.ExecutionId).NotEmpty();
        RuleFor(x => x.Registration).NotNull();
        RuleFor(x => x.OperatorPrincipal).NotNull();
    }
}
