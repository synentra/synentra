using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Synentra.Application;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Messaging;
using Synentra.Application.Common.Plugins;
using Synentra.Application.Common.Security;
using Synentra.Application.Features.Effects.Contracts;
using Synentra.Application.Features.Executions.Contracts;
using Synentra.Application.Features.Intent.Contracts;
using Synentra.Application.Features.Invocations.Normalization;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Application.Features.Obligations.Contracts;
using Synentra.Application.Features.Policies.Contracts;
using Synentra.Application.Features.Resources.Contracts;
using Synentra.Application.Features.Risk.Contracts;
using Synentra.Application.Features.Trust.Contracts;
using Synentra.Domain.Governance.Intent;
using Synentra.Infrastructure.Configuration;
using Synentra.Infrastructure.Dispatchers;
using Synentra.Infrastructure.Features.Adapters;
using Synentra.Infrastructure.Features.Effects;
using Synentra.Infrastructure.Features.Executions;
using Synentra.Infrastructure.Features.Identity;
using Synentra.Infrastructure.Features.Intent;
using Synentra.Infrastructure.Features.Invocations;
using Synentra.Infrastructure.Features.Manifests;
using Synentra.Infrastructure.Features.Obligations;
using Synentra.Infrastructure.Features.Policies;
using Synentra.Infrastructure.Features.Resources;
using Synentra.Infrastructure.Features.Risk;
using Synentra.Infrastructure.Features.Secrets;
using Synentra.Infrastructure.Features.Trust;
using Synentra.Infrastructure.Health;
using Synentra.Infrastructure.Networking;
using Synentra.Infrastructure.Plugins;
using Synentra.Persistence;

namespace Synentra.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSynentraInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSynentraApplication();
        services.AddSynentraPersistence();
        services.AddLogging();
        var options = configuration.GetSection("Synentra:Infrastructure").Get<InfrastructureOptions>() ?? new();

        if (string.IsNullOrWhiteSpace(options.KeyRingPath))
            throw new InvalidOperationException("A durable data-protection key-ring path is required.");

        services.AddOptions<InfrastructureOptions>().Bind(configuration.GetSection("Synentra:Infrastructure")).Validate(o => o.InitialTrustScore is >= 0 and <= 100 && o.QuarantineThreshold is >= 0 and <= 100 && o.HttpTimeoutSeconds > 0 && o.MaximumResponseBytes > 0, "Invalid infrastructure options.").ValidateOnStart();

        services.AddOptions<IdentityOptions>().Bind(configuration.GetSection("Synentra:Identity"));
        services.AddOptions<ExternalSecretOptions>().Bind(configuration.GetSection("Synentra:Secrets"));
        services.AddDataProtection().SetApplicationName("Synentra").PersistKeysToFileSystem(new DirectoryInfo(options.KeyRingPath));
        services.AddHttpClient("synentra-governed", c => c.Timeout = TimeSpan.FromSeconds(options.HttpTimeoutSeconds)).ConfigurePrimaryHttpMessageHandler(() => new GovernedHttpHandler(options));
        services.AddSingleton<IAdminPolicy, ConfiguredAdminPolicy>();
        services.AddSingleton<IBundleParser, BundleParser>();
        services.AddScoped<ManifestProvider>();
        services.AddScoped<IManifestProvider>(s => s.GetRequiredService<ManifestProvider>());
        services.AddScoped<IManifestProviderInternal>(s => s.GetRequiredService<ManifestProvider>());
        services.AddScoped<IRevisionProvider>(s => s.GetRequiredService<ManifestProvider>());
        services.AddSingleton<PluginCatalog>(s =>
        {
            var catalog = new PluginCatalog();
            foreach (var registration in s.GetServices<IPluginRegistration>())
                catalog.Register(registration);
            return catalog;
        });

        services.AddSingleton<IPluginResolver, PluginResolver>();

        void Plugin(string id, PluginFamily family, Type input, Type output, params string[] capabilities) => services.AddSingleton<IPluginRegistration>(new PluginRegistration(id, family, new[] { input }, new[] { output }, capabilities.ToHashSet()));

        Plugin("http", PluginFamily.TransportAdapter, typeof(AdmittedSnapshot), typeof(TransportReceipt), "request_response");
        Plugin("mcp", PluginFamily.TransportAdapter, typeof(AdmittedSnapshot), typeof(TransportReceipt), "request_response");
        Plugin("http-resource", PluginFamily.ResourceResolver, typeof(Domain.Invocations.ToolInvocation), typeof(ResourceResolutionResult), "resource-resolution");
        Plugin("configured-effect", PluginFamily.EffectResolver, typeof(Domain.Invocations.ToolInvocation), typeof(Domain.Effects.ValueObjects.EffectPlan), "bounded-effects");
        Plugin("http-readback", PluginFamily.OutcomeVerifier, typeof(Domain.Execution.ExecutionRecord), typeof(Domain.Execution.Evidence.OutcomeEvidence), "readback");
        Plugin("declarative", PluginFamily.PolicyEngine, typeof(PolicyInput), typeof(PolicyResult), "deny-overrides");
        Plugin("opa", PluginFamily.PolicyEngine, typeof(PolicyInput), typeof(PolicyResult), "rest-decision");
        services.AddScoped<DeclarativePolicyEngine>();
        services.AddScoped<OpaPolicyEngine>();
        services.AddScoped<IPolicyEngine>(s => options.PolicyProvider switch
        {
            "declarative" => s.GetRequiredService<DeclarativePolicyEngine>(),
            "opa" => s.GetRequiredService<OpaPolicyEngine>(),
            _ => throw new InvalidOperationException("Unknown policy provider.")});

        services.AddScoped<ITrustEngine, DbTrustEngine>();
        services.AddScoped<QuarantineService>();
        services.AddScoped<TrustAdministrationService>();
        services.AddScoped<IRiskEngine, DefaultRiskEngine>();
        services.AddScoped<IRiskFactorPlugin, TrustRiskFactor>();
        services.AddScoped<IIntentProvider, HttpIntentProvider>();
        services.AddScoped<IResourceResolver, HttpResourceResolver>();
        services.AddScoped<IEffectResolver, ConfiguredEffectResolver>();
        services.AddScoped<IOutcomeVerifier, HttpOutcomeVerifier>();
        services.AddScoped<IOperationRegistry, ManifestOperationRegistry>();
        // Register one normalizer per enum modality; combined flag values are not separate transports.
        foreach (var modality in Enum.GetValues<IntentModality>().Where(x => (int)x != 0 && (((int)x & ((int)x - 1)) == 0)))
        {
            var m = modality;
            services.AddScoped<IInvocationNormalizer>(s => new ConfiguredInvocationNormalizer(s.GetRequiredService<IManifestProvider>(), s.GetRequiredService<IRevisionProvider>(), m));
        }

        services.AddScoped<LocalPrincipalResolver>();
        services.AddSingleton<OidcPrincipalResolver>();
        services.AddScoped<LdapPrincipalResolver>();
        services.AddScoped<IPrincipalResolver>(s => s.GetRequiredService<IOptions<IdentityOptions>>().Value.Provider switch
        {
            "local" => s.GetRequiredService<LocalPrincipalResolver>(),
            "oidc" => s.GetRequiredService<OidcPrincipalResolver>(),
            "ldap" => s.GetRequiredService<LdapPrincipalResolver>(),
            _ => throw new InvalidOperationException("Unknown identity provider.")});

        services.AddScoped<EncryptedDbCredentialProvider>();
        services.AddScoped<ICredentialProvider>(s => s.GetRequiredService<EncryptedDbCredentialProvider>());
        services.AddScoped<CredentialProviderRouter>();
        var secrets = configuration.GetSection("Synentra:Secrets").Get<ExternalSecretOptions>() ?? new();
        if (secrets.VaultUrl is not null)
            services.AddScoped<ICredentialProvider, HashiCorpVaultCredentialProvider>();
        if (secrets.AzureVaultUrl is not null)
        {
            services.AddSingleton(new Azure.Security.KeyVault.Secrets.SecretClient(new Uri(secrets.AzureVaultUrl), new Azure.Identity.DefaultAzureCredential()));
            services.AddScoped<ICredentialProvider, AzureKeyVaultCredentialProvider>();
        }

        if (secrets.AwsRegion is not null)
        {
            services.AddSingleton<Amazon.SecretsManager.IAmazonSecretsManager>(new Amazon.SecretsManager.AmazonSecretsManagerClient(Amazon.RegionEndpoint.GetBySystemName(secrets.AwsRegion)));
            services.AddScoped<ICredentialProvider, AwsSecretsManagerCredentialProvider>();
        }

        services.AddScoped<IToolDispatcher, HttpToolDispatcher>();
        services.AddScoped<IToolDispatcher, McpToolDispatcher>();
        services.AddScoped<IExecutionSnapshotFactory, ExecutionSnapshotFactory>();
        services.AddScoped<IOutboxProcessor, OutboxProcessor>();
        services.AddScoped<ClaimRecoveryService>();
        services.AddScoped<IObligationHandler, ReviewObligationHandler>();
        services.AddScoped<IObligationHandler, TargetPreconditionObligationHandler>();
        if (options.StartOutboxWorker)
            services.AddHostedService<OutboxWorker>();
        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("synentra-database").AddCheck<ManifestHealthCheck>("synentra-manifest");
        services.AddScoped<IDispatcher, Dispatcher>();
        return services;
    }
}
