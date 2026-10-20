using FluentValidation;

namespace Synentra.Application.Features.TaskGrants.Commands.RevokeTaskGrant;

public sealed class RevokeTaskGrantCommandValidator : AbstractValidator<RevokeTaskGrantCommand>
{
    public RevokeTaskGrantCommandValidator()
    {
        RuleFor(x => x.GrantId).NotEmpty();
        RuleFor(x => x.By).NotNull();
    }
}
