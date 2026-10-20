using FluentValidation;
using Synentra.Application.Features.Invocations.Contracts;
using System.Text;

namespace Synentra.Application.Features.Invocations.Commands.SubmitInvocation;

public sealed class SubmitInvocationCommandValidator : AbstractValidator<SubmitInvocationCommand>
{
    public SubmitInvocationCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.SchemaVersion).Equal("synentra.invocation.v1");
        RuleFor(x => x.Request.OperationId).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Request.BindingId).MaximumLength(256).When(x => x.Request.BindingId is not null);
        RuleFor(x => x.Request.TaskGrantId).MaximumLength(256).When(x => x.Request.TaskGrantId is not null);
        RuleFor(x => x.Request.Arguments).Must(x => x.ValueKind is not System.Text.Json.JsonValueKind.Undefined).WithMessage("arguments is required.");
        RuleFor(x => x.Request).Must(x => ContextBytes(x) <= 8 * 1024).WithMessage("context exceeds 8 KiB.");
    }

    private static int ContextBytes(InvocationRequest request)
        => Encoding.UTF8.GetByteCount((request.Context?.WorkflowId ?? string.Empty) + (request.Context?.DeclaredPurpose ?? string.Empty));
}
