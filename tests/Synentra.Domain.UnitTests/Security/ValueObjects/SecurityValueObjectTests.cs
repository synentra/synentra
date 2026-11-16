using FluentAssertions;
using Synentra.Domain.Security;
using Synentra.Domain.Security.ValueObjects;

namespace Synentra.Domain.Tests;

public sealed class SecurityValueObjectTests
{
    [Fact]
    public void CanonicalPrincipal_ShouldPreserveConstructorValues()
    {
        var scopes = new HashSet<string>(["inventory.read", "inventory.update"], StringComparer.Ordinal);
        var claims = new Dictionary<string, string> { ["department"] = "operations" };
        var principal = new CanonicalPrincipal("agent-1", "tenant-a", "https://issuer", "subject-1",
            scopes, claims, PrincipalType.Agent);

        principal.AgentId.Should().Be("agent-1");
        principal.SecurityScope.Should().Be("tenant-a");
        principal.Issuer.Should().Be("https://issuer");
        principal.Subject.Should().Be("subject-1");
        principal.Scopes.Should().BeSameAs(scopes);
        principal.Claims.Should().BeSameAs(claims);
        principal.Type.Should().Be(PrincipalType.Agent);
    }

    [Fact]
    public void ExecutionIdentity_ShouldExposeDelegationAndIssuerValues()
    {
        var identity = new ExecutionIdentity("agent", "scope", "authenticated-subject", "user-1", "issuer-1");
        identity.AgentId.Should().Be("agent");
        identity.SecurityScope.Should().Be("scope");
        identity.AuthenticatedSubject.Should().Be("authenticated-subject");
        identity.DelegatedUserId.Should().Be("user-1");
        identity.Issuer.Should().Be("issuer-1");

        var withoutDelegation = new ExecutionIdentity("agent", "scope", "subject", null, null);
        withoutDelegation.DelegatedUserId.Should().BeNull();
        withoutDelegation.Issuer.Should().BeNull();
    }

    [Fact]
    public void SecretProviderRef_ShouldUseDefaultFieldAndAllowCustomField()
    {
        var defaultField = new SecretProviderRef("localvault", "inventory-api", "v2");
        var customField = new SecretProviderRef("azure-key-vault", "crm", "latest", "password");

        defaultField.ProviderId.Should().Be("localvault");
        defaultField.SecretPath.Should().Be("inventory-api");
        defaultField.Version.Should().Be("v2");
        defaultField.Field.Should().Be("value");
        customField.Field.Should().Be("password");
    }

    [Fact]
    public void CredentialBinding_ShouldNormalizeOperationSetAndDefaultAuthorityVersion()
    {
        var operations = new HashSet<string>(["inventory.read", "inventory.update"], StringComparer.Ordinal);
        var secret = new SecretProviderRef("localvault", "inventory-api", "v2");
        var binding = new CredentialBinding("binding-1", "inventory-api", "agent-1", "development",
            operations, secret, "v2");
        operations.Add("external.added");

        binding.Id.Should().Be("binding-1");
        binding.TargetRef.Should().Be("inventory-api");
        binding.AllowedPrincipal.Should().Be("agent-1");
        binding.DelegationScope.Should().Be("development");
        binding.AllowedOperations.Should().BeEquivalentTo("inventory.read", "inventory.update");
        binding.SecretRef.Should().Be(secret);
        binding.Version.Should().Be("v2");
        binding.Revoked.Should().BeFalse();
        binding.AuthorityVersion.Should().Be("v2");
    }

    [Fact]
    public void CredentialBinding_ShouldHonorExplicitAuthorityVersionAndRevocation()
    {
        var secret = new SecretProviderRef("localvault", "path", "secret-v1");
        var binding = CreateBinding(authorityVersion: "authority-v7", revoked: true, secret: secret);
        binding.AuthorityVersion.Should().Be("authority-v7");
        binding.Revoked.Should().BeTrue();
        binding.SecretRef.Should().Be(secret);
    }

    [Fact]
    public void HasSameAuthority_ShouldReturnTrueForSameActiveAuthorityEvenIfOperationOrderDiffers()
    {
        var left = CreateBinding(operations: ["read", "write"]);
        var right = CreateBinding(operations: ["write", "read"]);
        left.HasSameAuthority(right).Should().BeTrue();
    }

    [Fact]
    public void HasSameAuthority_ShouldReturnFalseWhenEitherBindingIsRevoked()
    {
        var active = CreateBinding();
        active.HasSameAuthority(CreateBinding(revoked: true)).Should().BeFalse();
        CreateBinding(revoked: true).HasSameAuthority(active).Should().BeFalse();
    }

    [Fact]
    public void HasSameAuthority_ShouldReturnFalseWhenAnAuthorityDimensionDiffers()
    {
        var baseline = CreateBinding();
        var differentBindings = new[]
        {
            CreateBinding(id: "different-id"),
            CreateBinding(authorityVersion: "different-authority-version"),
            CreateBinding(targetRef: "different-target"),
            CreateBinding(principal: "different-principal"),
            CreateBinding(delegation: "different-delegation"),
            CreateBinding(operations: ["read", "different-operation"])
        };

        foreach (var other in differentBindings)
            baseline.HasSameAuthority(other).Should().BeFalse();
    }

    private static CredentialBinding CreateBinding(
        string id = "binding", string targetRef = "target", string principal = "agent",
        string delegation = "scope", IEnumerable<string>? operations = null,
        bool revoked = false, string authorityVersion = "authority-v1", SecretProviderRef? secret = null)
        => new(id, targetRef, principal, delegation,
            new HashSet<string>(operations ?? new[] { "read", "write" }, StringComparer.Ordinal),
            secret ?? new SecretProviderRef("localvault", "path", "v1"), "v1", revoked, authorityVersion);
}
