using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
using Synentra.Infrastructure.Configuration.Approvals;
using Synentra.Infrastructure.Configuration.BackgroundJobs;
using Synentra.Infrastructure.Configuration.Cors;
using Synentra.Infrastructure.Configuration.DataProtection;
using Synentra.Infrastructure.Configuration.Identity;
using Synentra.Infrastructure.Configuration.Logging;
using Synentra.Infrastructure.Configuration.OpenTelemetry;
using Synentra.Infrastructure.Configuration.OutboundHttp;
using Synentra.Infrastructure.Configuration.Persistence;
using Synentra.Infrastructure.Configuration.Policy;
using Synentra.Infrastructure.Configuration.ResponseRetention;
using Synentra.Infrastructure.Configuration.Secrets;
using Synentra.Infrastructure.Configuration.Security;
using Synentra.Infrastructure.Configuration.Semantic;
using Synentra.Infrastructure.Configuration.Trust;
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
using Synentra.Persistence.Features.Security;
using System.Security.Cryptography.X509Certificates;

namespace Synentra.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSynentraConfiguration(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Register approvals configuration sections and validators
        services.AddSingleton<IValidateOptions<ApprovalsConfiguration>, ApprovalsConfigurationValidator>();
        services.AddConfiguration<ApprovalsConfiguration>(configuration);

        // Register background jobs configuration sections and validators
        services.AddSingleton<IValidateOptions<BackgroundJobsConfiguration>, BackgroundJobsConfigurationValidator>();
        services.AddConfiguration<BackgroundJobsConfiguration>(configuration);

        // Register CORS configuration sections and validators
        services.AddSingleton<IValidateOptions<CorsConfiguration>, CorsConfigurationValidator>();
        services.AddConfiguration<CorsConfiguration>(configuration);

        // Register data protection configuration sections and validators
        services.AddSingleton<IValidateOptions<DataProtectionConfiguration>, DataProtectionConfigurationValidator>();
        services.AddConfiguration<DataProtectionConfiguration>(configuration);

        // Register response retention configuration sections and validators
        services.AddConfiguration<ResponseRetentionConfiguration>(configuration);

        // Register identity configuration sections and validators
        services.AddSingleton<IValidateOptions<IdentityConfiguration>, IdentityConfigurationValidator>();
        services.AddConfiguration<IdentityConfiguration>(configuration);

        // Register logging configuration sections and validators
        services.AddSingleton<IValidateOptions<LoggingConfiguration>, LoggingConfigurationValidator>();
        services.AddConfiguration<LoggingConfiguration>(configuration);

        // Register OpenTelemetry configuration sections and validators
        services.AddSingleton<IValidateOptions<OpenTelemetryConfiguration>, OpenTelemetryConfigurationValidator>();
        services.AddConfiguration<OpenTelemetryConfiguration>(configuration);

        // Register outbound HTTP configuration sections and validators
        services.AddSingleton<IValidateOptions<OutboundHttpConfiguration>, OutboundHttpConfigurationValidator>();
        services.AddConfiguration<OutboundHttpConfiguration>(configuration);

        // Register persistence configuration sections and validators
        services.AddSingleton<IValidateOptions<PersistenceConfiguration>, PersistenceConfigurationValidator>();
        services.AddConfiguration<PersistenceConfiguration>(configuration);

        // Register policy configuration sections and validators
        services.AddSingleton<IValidateOptions<PolicyConfiguration>, PolicyConfigurationValidator>();
        services.AddConfiguration<PolicyConfiguration>(configuration);

        // Register secret management configuration sections and validators
        services.AddConfiguration<SecretsConfiguration>(configuration);

        // Register semantic configuration sections and validators
        //services.AddSingleton<IValidateOptions<SemanticConfiguration>, SemanticConfigurationValidator>();
        services.AddConfiguration<SemanticConfiguration>(configuration);
        
        // Register trust configuration sections and validators
        services.AddSingleton<IValidateOptions<TrustConfiguration>, TrustConfigurationValidator>();
        services.AddConfiguration<TrustConfiguration>(configuration);

        return services;
    }

    private static IServiceCollection AddConfiguration<T>(
        this IServiceCollection services,
        IConfiguration configuration)
        where T : class, ISynentraConfiguration
    {
        services.AddOptions<T>()
            .Bind(configuration.GetSection(T.SectionName))
            .ValidateOnStart();

        return services;
    }

    public static IServiceCollection AddSynentraInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSynentraApplication();
        services.AddSynentraConfiguration(configuration);
        services.AddLogging();

        if (services.Any(d => d.ServiceType == typeof(KeyRingCertificateLifetime)))
            throw new InvalidOperationException("Register the Synentra key ring only once.");

        using var scope = services.BuildServiceProvider().CreateScope();
        var dataProtectionConfig = scope.ServiceProvider.GetRequiredService<IOptions<DataProtectionConfiguration>>();

        var path = KeyRingSecurity.ResolvePath(dataProtectionConfig.Value.KeyRingPath);
        var applicationName = dataProtectionConfig.Value.ApplicationName;
        var requireEncryption = dataProtectionConfig.Value.RequireEncryptedKeyRing;
        var hasCertificate = !string.IsNullOrWhiteSpace(dataProtectionConfig.Value.Certificate.Path) ||
            !string.IsNullOrWhiteSpace(dataProtectionConfig.Value.Certificate.Thumbprint);

        X509Certificate2? active = null;
        PrivateKeyRingRepository repository;
        try
        {
            // Certificates are bootstrapped before touching the key-ring directory.
            if (hasCertificate)
            {
                active = KeyRingCertificates.LoadActive(dataProtectionConfig.Value);
            }
            repository = new PrivateKeyRingRepository(path, requireEncryption);
            _ = repository.GetAllElements();
        }
        catch
        {
            active?.Dispose();
            throw;
        }

        // Register a factory, not an already-created IDisposable instance, so DI owns disposal.
        services.AddSingleton(_ => new KeyRingCertificateLifetime(active));
        services.AddSingleton(repository);
        var builder = services.AddDataProtection()
            .SetApplicationName(applicationName)
            .SetDefaultKeyLifetime(dataProtectionConfig.Value.DefaultKeyLifetime);
        services.AddOptions<KeyManagementOptions>()
            .Configure<PrivateKeyRingRepository>((keyOptions, repo) => keyOptions.XmlRepository = repo);
        if (active is not null)
        {
            builder.ProtectKeysWithCertificate(active);
            builder.UnprotectKeysWithAnyCertificate(active);
        }

        // Host.StartAsync awaits this work. Do not move it into BackgroundService.ExecuteAsync.
        services.AddSingleton<IHostedService>(sp =>
        {
            var lifetime = sp.GetRequiredService<KeyRingCertificateLifetime>();
            return new KeyRingStartupValidator(
                sp.GetRequiredService<IKeyManager>(),
                sp.GetRequiredService<IDataProtectionProvider>(),
                sp.GetRequiredService<PrivateKeyRingRepository>(), lifetime);
        });






        var outboundHttpConfig = scope.ServiceProvider.GetRequiredService<IOptions<OutboundHttpConfiguration>>();
        services.AddHttpClient("synentra-governed", c => c.Timeout = outboundHttpConfig.Value.Timeout ?? TimeSpan.FromSeconds(30))
                .ConfigurePrimaryHttpMessageHandler(() => new GovernedHttpHandler(outboundHttpConfig));

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

        Plugin("http", PluginFamily.TransportAdapter, typeof(AdmittedSnapshot), typeof(TransportReceipt), "request_response", "idempotency", "conditional-headers", "bounded-response", "ssrf-guard");
        Plugin("mcp", PluginFamily.TransportAdapter, typeof(AdmittedSnapshot), typeof(TransportReceipt), "request_response", "streamable-http", "tools", "pagination", "cancellation", "review-resume");
        Plugin("http-resource", PluginFamily.ResourceResolver, typeof(Domain.Invocations.ToolInvocation), typeof(ResourceResolutionResult), "resource-resolution");
        Plugin("configured-effect", PluginFamily.EffectResolver, typeof(Domain.Invocations.ToolInvocation), typeof(Domain.Effects.ValueObjects.EffectPlan), "bounded-effects");
        Plugin("http-readback", PluginFamily.OutcomeVerifier, typeof(Domain.Execution.ExecutionRecord), typeof(Domain.Execution.Evidence.OutcomeEvidence), "readback");
        Plugin("declarative", PluginFamily.PolicyEngine, typeof(PolicyInput), typeof(PolicyResult), "deny-overrides");
        Plugin("opa", PluginFamily.PolicyEngine, typeof(PolicyInput), typeof(PolicyResult), "rest-decision");

        var policyConfig = scope.ServiceProvider.GetRequiredService<IOptions<PolicyConfiguration>>();

        services.AddScoped<DeclarativePolicyEngine>();
        services.AddScoped<OpaPolicyEngine>();
        services.AddScoped<IPolicyEngine>(s => policyConfig.Value.DefaultProvider.ToLowerInvariant() switch
        {
            "declarative" => s.GetRequiredService<DeclarativePolicyEngine>(),
            "opa" => s.GetRequiredService<OpaPolicyEngine>(),
            _ => throw new InvalidOperationException("Unknown policy provider.")
        });

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

        services.AddSingleton<OidcPrincipalResolver>();
        services.AddScoped<LdapPrincipalResolver>();
        services.AddScoped<IPrincipalResolver>(s => s.GetRequiredService<IOptions<IdentityConfiguration>>().Value.DefaultProvider.ToLowerInvariant() switch
        {
            "oidc" => s.GetRequiredService<OidcPrincipalResolver>(),
            "ldap" => s.GetRequiredService<LdapPrincipalResolver>(),
            _ => throw new InvalidOperationException("Unknown identity provider.")
        });

        var secretsConfig = scope.ServiceProvider.GetRequiredService<IOptions<SecretsConfiguration>>();

        services.AddScoped<EncryptedDbCredentialProvider>();
        if (secretsConfig.Value.Providers.EncryptedDatabase.EncryptedDatabaseEnabled)
            services.AddScoped<ICredentialProvider>(s => s.GetRequiredService<EncryptedDbCredentialProvider>());

        services.AddScoped<ICredentialProvider>(s => s.GetRequiredService<EncryptedDbCredentialProvider>());
        services.AddScoped<CredentialProviderRouter>();

        services.AddScoped<IRuntimeCredentialResolver>(s => s.GetRequiredService<CredentialProviderRouter>());
        services.AddScoped<SecretStateStore>();
        var secretPlan = configuration.GetSection("SecretPlan").Get<SecretPlan>() ?? new();
        secretPlan.Cache.Validate();
        services.AddSingleton(new SecretMaterialCache(secretPlan.Cache));


        if (secretsConfig.Value.Providers.HashiCorpVault.VaultUrl is not null)
            services.AddScoped<ICredentialProvider, HashiCorpVaultCredentialProvider>();
        if (secretsConfig.Value.Providers.AzureKeyVault.AzureVaultUrl is not null)
        {
            services.AddSingleton(new Azure.Security.KeyVault.Secrets.SecretClient(new Uri(secretsConfig.Value.Providers.AzureKeyVault.AzureVaultUrl), new Azure.Identity.DefaultAzureCredential()));
            services.AddScoped<ICredentialProvider, AzureKeyVaultCredentialProvider>();
        }

        if (secretsConfig.Value.Providers.AwsSecrets.AwsRegion is not null)
        {
            services.AddSingleton<Amazon.SecretsManager.IAmazonSecretsManager>(new Amazon.SecretsManager.AmazonSecretsManagerClient(Amazon.RegionEndpoint.GetBySystemName(secretsConfig.Value.Providers.AwsSecrets.AwsRegion)));
            services.AddScoped<ICredentialProvider, AwsSecretsManagerCredentialProvider>();
        }

        services.AddScoped<IToolDispatcher, HttpToolDispatcher>();
        services.AddScoped<IToolDispatcher, McpToolDispatcher>();
        services.AddScoped<IExecutionSnapshotFactory, ExecutionSnapshotFactory>();
        services.AddScoped<IOutboxProcessor, OutboxProcessor>();
        services.AddScoped<ClaimRecoveryService>();
        services.AddScoped<IObligationHandler, ReviewObligationHandler>();
        services.AddScoped<IObligationHandler, TargetPreconditionObligationHandler>();

        var outboxConfig = scope.ServiceProvider.GetRequiredService<IOptions<OutboxConfiguration>>();

        if (outboxConfig.Value.Enabled == true)
            services.AddHostedService<OutboxWorker>();

        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("synentra-database").AddCheck<ManifestHealthCheck>("synentra-manifest");
        services.AddScoped<IDispatcher, Dispatcher>();
        return services;
    }
}
