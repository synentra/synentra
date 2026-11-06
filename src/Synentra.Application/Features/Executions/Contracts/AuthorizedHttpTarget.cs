namespace Synentra.Application.Features.Executions.Contracts;

public sealed record AuthorizedHttpTarget(
    string TargetId,
    Uri EndpointUri,
    Uri RequestUri,
    string ManifestRevision,
    bool RequiresHttps)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(TargetId)
        && !string.IsNullOrWhiteSpace(ManifestRevision)
        && EndpointUri.IsAbsoluteUri
        && RequestUri.IsAbsoluteUri
        && EndpointUri.Scheme is "http" or "https"
        && RequestUri.Scheme is "http" or "https"
        && string.IsNullOrEmpty(EndpointUri.UserInfo)
        && string.IsNullOrEmpty(RequestUri.UserInfo)
        && string.IsNullOrEmpty(EndpointUri.Fragment)
        && string.IsNullOrEmpty(RequestUri.Fragment)
        && string.Equals(
            EndpointUri.GetLeftPart(UriPartial.Authority),
            RequestUri.GetLeftPart(UriPartial.Authority),
            StringComparison.OrdinalIgnoreCase);

    public bool Matches(Uri actualUri)
    {
        return string.Equals(actualUri.Scheme, RequestUri.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(actualUri.GetLeftPart(UriPartial.Authority), RequestUri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase)
            && string.Equals(actualUri.AbsolutePath, RequestUri.AbsolutePath, StringComparison.Ordinal)
            && string.Equals(actualUri.Query, RequestUri.Query, StringComparison.Ordinal);
    }
}
