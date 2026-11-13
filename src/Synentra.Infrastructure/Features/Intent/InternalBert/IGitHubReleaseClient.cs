using Octokit;

namespace Synentra.Infrastructure.Intent.Providers.InternalBert;

public interface IGitHubReleaseClient
{
    Task<Release> GetLatestReleaseAsync(string owner, string repo);
}
