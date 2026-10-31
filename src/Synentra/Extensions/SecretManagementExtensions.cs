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
        if (builder.Environment.IsProduction() && builder.Configuration.GetValue<bool?>("DataProtection:RequireEncryptedKeyRing") == false)
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

        configuration.AddInMemoryCollection(values);
        //scope.ServiceProvider.GetRequiredService<StartupSecretState>().Publish(identity, expires, oidc);
    }
}