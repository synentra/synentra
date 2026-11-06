using Microsoft.Extensions.Options;
using Synentra.Application.Common.Security;
using Synentra.Domain.Security.ValueObjects;
using Synentra.Infrastructure.Configuration.Secrets;
using Synentra.Infrastructure.Configuration.Identity;
using Synentra.Infrastructure.Features.Secrets;

namespace Synentra.Extensions;

public static class SecretManagementExtensions
{
    public static async Task<WebApplicationBuilder> AddSynentraSecretManagementAsync(
        this WebApplicationBuilder builder,
        CancellationToken ct = default)
    {
        if (builder.Environment.IsProduction() && builder.Configuration.GetValue<bool?>("DataProtection:RequireEncryptedKeyRing") == false)
            throw new InvalidOperationException("Production requires an encrypted key ring.");

        builder.Services.AddSingleton(await BootstrapSecrets.ApplyAsync(builder.Configuration, builder.Environment.IsDevelopment(), ct));
        return builder;
    }

    public static async Task InitializeStartupSecretsAsync(
        this WebApplication app,
        ConfigurationManager configuration,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(configuration);

        await using var scope = app.Services.CreateAsyncScope();

        var services = scope.ServiceProvider;
        var plan = services.GetRequiredService<SecretsConfiguration>();
        var state = services.GetRequiredService<StartupSecretState>();

        plan.Validate();

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);

        timeout.CancelAfter(plan.Resolution.Timeout);

        var token = timeout.Token;

        var providers = services
            .GetServices<ICredentialProvider>()
            .ToArray();

        var values = new Dictionary<string, string?>(
            StringComparer.OrdinalIgnoreCase);

        DateTimeOffset? earliestExpiry = null;

        foreach (var (name, entry) in plan.Bindings)
        {
            if (entry.Tier != SecretTier.Startup)
                continue;

            token.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(entry.ConfigurationKey))
                throw SecretsConfiguration.Invalid();

            string value;

            if (entry.Provider is "env" or "file")
            {
                value = BootstrapSecrets.Read(entry);
            }
            else
            {
                var matches = providers
                    .Where(provider => string.Equals(
                        provider.ProviderId,
                        entry.Provider,
                        StringComparison.Ordinal))
                    .ToArray();

                if (matches.Length != 1)
                {
                    throw new InvalidOperationException(
                        $"Startup secret '{name}' requires exactly one " +
                        $"registered provider named '{entry.Provider}'.");
                }

                var binding = new CredentialBinding(
                    name,
                    "startup",
                    "host",
                    "host",
                    new HashSet<string>(StringComparer.Ordinal)
                    {
                        "startup"
                    },
                    entry.Reference,
                    "startup");

                var credential = await matches[0].ResolveAsync(
                    binding,
                    token);

                token.ThrowIfCancellationRequested();

                SecretPayload.Validate(credential);

                value = credential.Value;

                if (credential.ExpiresAt is { } expiry &&
                    (earliestExpiry is null ||
                     expiry < earliestExpiry.Value))
                {
                    earliestExpiry = expiry;
                }
            }

            if (!values.TryAdd(entry.ConfigurationKey, value))
            {
                throw new InvalidOperationException(
                    "Multiple startup secrets target the same " +
                    "configuration key.");
            }
        }

        token.ThrowIfCancellationRequested();

        // Validate the merged configuration before applying it.
        var candidate = new ConfigurationBuilder()
            .AddConfiguration(configuration)
            .AddInMemoryCollection(values)
            .Build();

        var identity = candidate
            .GetSection(IdentityConfiguration.SectionName)
            .Get<IdentityConfiguration>()
            ?? throw new InvalidOperationException(
                "Identity configuration is missing.");

        // Direct configuration binding does not run options validators.
        foreach (var validator in services
                     .GetServices<IValidateOptions<IdentityConfiguration>>())
        {
            var result = validator.Validate(
                Options.DefaultName,
                identity);

            if (result.Failed)
            {
                // Do not include validation details that might expose
                // resolved secret values.
                throw new InvalidOperationException(
                    "Identity configuration is invalid after " +
                    "startup secret resolution.");
            }
        }

        token.ThrowIfCancellationRequested();

        // Earlier credentials may expire while later ones are resolved.
        if (earliestExpiry is { } expiration &&
            expiration <= DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException(
                "A mandatory startup secret expired during initialization.");
        }

        configuration.AddInMemoryCollection(values);

        // Publish last. Any failure above leaves startup blocked.
        // OIDC remains owned by its existing DI registration.
        state.Publish(identity, earliestExpiry);
    }
}