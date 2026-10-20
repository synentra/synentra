using Synentra.Domain.Security.ValueObjects;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Features.Invocations.Admission;
using Synentra.Application.Features.Invocations.Commands.ResumeInvocation;

namespace Synentra.Application.Features.Invocations.Resume;

public sealed class ResumeService
{
    private readonly IDispatcher _dispatcher;
    public ResumeService(IDispatcher dispatcher) => _dispatcher = dispatcher;

    public Task<AdmissionResult> ResumeAsync(
        string invocationId,
        CanonicalPrincipal caller, 
        CancellationToken ct) 
        => _dispatcher.Dispatch(new ResumeInvocationCommand(invocationId, caller), ct);
}
