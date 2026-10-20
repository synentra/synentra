namespace Synentra.Persistence;

public interface IDatabaseInitializer
{
    Task EnsureDatabaseCreatedAsync(CancellationToken cancellationToken = default);
}