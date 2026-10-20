using FluentValidation;
using System.Text;

namespace Synentra.Application.Features.Invocations.Commands.SimulateInvocation;

public sealed class SimulateInvocationCommandValidator : AbstractValidator<SimulateInvocationCommand>
{
    public SimulateInvocationCommandValidator()
    {
        RuleFor(x => x.Request.SchemaVersion).Equal("synentra.invocation.v1");
        RuleFor(x => x.Request.OperationId).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Request.BindingId).MaximumLength(256).When(x => x.Request.BindingId is not null);
        RuleFor(x => x.Request)
            .Must(x => Encoding.UTF8.GetByteCount((x.Context?.WorkflowId ?? string.Empty) + (x.Context?.DeclaredPurpose ?? string.Empty)) <= 8 * 1024)
            .WithMessage("context exceeds 8 KiB.");
    }
}
