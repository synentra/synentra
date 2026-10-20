namespace Synentra.Persistence;

public interface IDbContextScope
{
    SynentraDbContext Current { get; }
}