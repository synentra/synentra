using FluentValidation;

namespace Synentra.Application.Features.TaskGrants.Commands.IssueTaskGrant;

public sealed class IssueTaskGrantCommandValidator : AbstractValidator<IssueTaskGrantCommand>
{
    public IssueTaskGrantCommandValidator()
    {
        RuleFor(x => x.Issuer).NotNull();
        RuleFor(x => x.Requested).NotNull();
        RuleFor(x => x.IssuerCeilings).NotNull();
    }
}
