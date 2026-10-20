namespace Synentra.Application.Features.Manifests.Contracts;

public interface IRevisionProvider
{
    string ActiveRevision { get; }
    Task ReloadAsync(CancellationToken ct);
}
