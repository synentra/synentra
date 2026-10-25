using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Synentra.Application.Common.Security;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Infrastructure.Configuration.Security;
using Synentra.Infrastructure.Features.Identity;
using Synentra.Infrastructure.Features.Secrets;
using Synentra.Persistence;
using Synentra.Persistence.Entities;

namespace Synentra.Extensions;

public static class SecretManagementExtensions
{
    public static async Task<WebApplicationBuilder> AddSynentraSecretManagementAsync(this WebApplicationBuilder builder, CancellationToken ct = default)
    {
        if (builder.Environment.IsProduction() && builder.Configuration.GetValue<bool?>("Synentra:Infrastructure:RequireEncryptedKeyRing") == false)
            throw new InvalidOperationException("Production requires an encrypted key ring.");
        builder.Services.AddSingleton(await BootstrapSecrets.ApplyAsync(builder.Configuration, builder.Environment.IsDevelopment(), ct));
        return builder;
    }

    public static async Task InitializeStartupSecretsAsync(this WebApplication app, ConfigurationManager configuration, CancellationToken ct)
    {
        using var scope = app.Services.CreateScope();
        var plan = scope.ServiceProvider.GetRequiredService<SecretPlan>();
        plan.Validate();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(plan.TimeoutSeconds));
        var database = scope.ServiceProvider.GetRequiredService<SynentraDbContext>();
        if (!await database.Database.CanConnectAsync(timeout.Token)) throw new InvalidOperationException("Secret database unavailable.");
        await database.Secrets.AsNoTracking().Take(1).ToArrayAsync(timeout.Token);
        await database.CredentialBindings.AsNoTracking().Take(1).ToArrayAsync(timeout.Token);
        await database.Set<SecretStateEntity>().AsNoTracking().Take(1).ToArrayAsync(timeout.Token);
        var providers = scope.ServiceProvider.GetServices<ICredentialProvider>().ToArray();
        var values = new Dictionary<string, string?>();
        DateTimeOffset? expires = null;
        foreach (var (name, entry) in plan.Entries.Where(e => e.Value.Tier == SecretTier.Startup))
        {
            if (entry.Provider is "env" or "file") values.Add(entry.ConfigurationKey!, BootstrapSecrets.Read(entry));
            else
            {
                var provider = providers.SingleOrDefault(p => p.ProviderId == entry.Provider) ?? throw SecretPlan.Invalid();
                var binding = new CredentialBinding(name, "startup", "host", "host", new HashSet<string> { "startup" }, entry.Reference, "startup");
                var secret = await provider.ResolveAsync(binding, timeout.Token);
                SecretPayload.Validate(secret);
                if (secret.ExpiresAt is { } expiry && (expires is null || expiry < expires)) expires = expiry;
                values.Add(entry.ConfigurationKey!, secret.Value);
            }
        }
        var candidate = new ConfigurationBuilder().AddConfiguration(configuration).AddInMemoryCollection(values).Build();
        using var candidateLifetime = candidate as IDisposable;
        var identity = candidate.GetSection("Identity").Get<IdentityConfiguration>() ?? new();
        bool Declared(string key) => plan.IsDevelopment || plan.Entries.Values.Any(e => e.Tier == SecretTier.Startup && e.ConfigurationKey == key);
        void ValidateKey(string key)
        {
            try { if (Convert.FromBase64String(key).Length < 32) throw SecretPlan.Invalid(); }
            catch { throw new InvalidOperationException("Startup signing keys must be base64 with at least 256 bits."); }
        }

        if (identity.DefaultProvider.ToLowerInvariant() == "ldap" 
            && (!Declared("Identity:Providers:Ldap:LdapServicePassword") 
            || string.IsNullOrWhiteSpace(identity.Providers.Ldap.ServicePassword))) 
            throw SecretPlan.Invalid();

        if (identity.DefaultProvider.ToLowerInvariant() == "oidc" 
            && (!Uri.TryCreate(identity.Providers.Oidc.Authority, UriKind.Absolute, out var authority) 
            || authority.Scheme != "https" 
            || string.IsNullOrWhiteSpace(identity.Providers.Oidc.Audience))) 
            throw SecretPlan.Invalid();

        if (identity.DefaultProvider.ToLowerInvariant() is not ("ldap" or "oidc")) 
            throw SecretPlan.Invalid();

        if (candidate["Policy:DefaultProvider"] == "opa" 
            && (!Declared("Policy:Providers:Opa:OpaBearerToken") 
            || string.IsNullOrWhiteSpace(candidate["Policy:Providers:Opa:OpaBearerToken"]))) 
            throw SecretPlan.Invalid();

        var protection = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>();
        if (protection is not null)
        {
            var protector = protection.CreateProtector("DataProtection:StartupKeyRingProbe.v1");
            var probe = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
            if (protector.Unprotect(protector.Protect(probe)) != probe) 
                throw new InvalidOperationException("Key-ring readiness probe failed.");
        }
        OidcPrincipalResolver? oidc = null;
        if (identity.DefaultProvider == "oidc")
        {
            oidc = new OidcPrincipalResolver(Microsoft.Extensions.Options.Options.Create(identity));
            try { await oidc.WarmupAsync(timeout.Token); } catch { oidc.Dispose(); throw; }
        }

        configuration.AddInMemoryCollection(values);
        scope.ServiceProvider.GetRequiredService<StartupSecretState>().Publish(identity, expires, oidc);
    }
}