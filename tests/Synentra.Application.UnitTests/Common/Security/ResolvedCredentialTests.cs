using System.Net.Http.Headers;
using FluentAssertions;
using Synentra.Application.Common.Security;

namespace Synentra.Application.UnitTests.Common.Security;

public sealed class ResolvedCredentialTests
{
    [Fact]
    public void ToString_redacts_secret()
    {
        var credential = new ResolvedCredential("api-key", "secret-value", null);

        credential.ToString().Should().Be("ResolvedCredential([REDACTED])");
        credential.ToString().Should().NotContain("secret-value");
    }

    [Fact]
    public void ToAuthHeader_uses_default_bearer_scheme()
    {
        var credential = new ResolvedCredential("token", "secret-value", null);

        credential.ToAuthHeader().Should().Be(new AuthenticationHeaderValue("Bearer", "secret-value"));
    }

    [Fact]
    public void ToAuthHeader_uses_custom_scheme()
    {
        var credential = new ResolvedCredential("token", "secret-value", null, "Basic");

        credential.ToAuthHeader().Scheme.Should().Be("Basic");
        credential.ToAuthHeader().Parameter.Should().Be("secret-value");
    }
}
