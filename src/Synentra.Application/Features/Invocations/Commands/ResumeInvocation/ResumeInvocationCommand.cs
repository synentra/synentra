using Synentra.Domain.Security.ValueObjects;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Invocations.Admission;

namespace Synentra.Application.Features.Invocations.Commands.ResumeInvocation;

public sealed record ResumeInvocationCommand(
    string InvocationId, 
    CanonicalPrincipal Caller) : ICommand<AdmissionResult>;
