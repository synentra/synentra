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
using Synentra.Infrastructure.Configuration.Approvals;
using Synentra.Infrastructure.Configuration.BackgroundJobs;
using Synentra.Infrastructure.Configuration.Cors;
using Synentra.Infrastructure.Configuration.DataProtection;
using Synentra.Infrastructure.Configuration.Logging;
using Synentra.Infrastructure.Configuration.OpenTelemetry;
using Synentra.Infrastructure.Configuration.OutboundHttp;
using Synentra.Infrastructure.Configuration.Persistence;
using Synentra.Infrastructure.Configuration.Policy;
using Synentra.Infrastructure.Configuration.SecretManagement;
using Synentra.Infrastructure.Configuration.Security;
using Synentra.Infrastructure.Configuration.Semantic;
using Synentra.Infrastructure.Configuration.Trust;

namespace Synentra.Infrastructure;

public static class DependencyInjection
{
    private const string ApprovalsConfigurationName = "Approvals";
    private const string BackgroundJobsConfigurationName = "BackgroundJobs";
    private const string CorsConfigurationName = "Cors";
    private const string DataProtectionConfigurationName = "DataProtection";
    private const string IdentityConfigurationName = "Identity";
    private const string LoggingConfigurationName = "Logging";
    private const string OpenTelemetryConfigurationName = "OpenTelemetry";
    private const string OutboundHttpConfigurationName = "OutboundHttp";
    private const string PersistenceConfigurationName = "Persistence";
    private const string PolicyConfigurationName = "Policy";
    private const string SecretManagementConfigurationName = "SecretManagement";
    private const string SemanticConfigurationName = "Semantic";
    private const string TrustConfigurationName = "Trust";

    public static IServiceCollection AddSynentraConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ApprovalsConfiguration>(configuration.GetSection(ApprovalsConfigurationName));
        services.Configure<BackgroundJobsConfiguration>(configuration.GetSection(BackgroundJobsConfigurationName));
        services.Configure<CorsConfiguration>(configuration.GetSection(CorsConfigurationName));
        services.Configure<DataProtectionConfiguration>(configuration.GetSection(DataProtectionConfigurationName));
        services.Configure<IdentityConfiguration>(configuration.GetSection(IdentityConfigurationName));
        services.Configure<LoggingConfiguration>(configuration.GetSection(LoggingConfigurationName));
        services.Configure<OpenTelemetryConfiguration>(configuration.GetSection(OpenTelemetryConfigurationName));
        services.Configure<OutboundHttpConfiguration>(configuration.GetSection(OutboundHttpConfigurationName));
        services.Configure<PersistenceConfiguration>(configuration.GetSection(PersistenceConfigurationName));
        services.Configure<PolicyConfiguration>(configuration.GetSection(PolicyConfigurationName));
        services.Configure<SecretManagementConfiguration>(configuration.GetSection(SecretManagementConfigurationName));
        services.Configure<SemanticConfiguration>(configuration.GetSection(SemanticConfigurationName));
        services.Configure<TrustConfiguration>(configuration.GetSection(TrustConfigurationName));

        return services;
    }

    public static IServiceCollection AddSynentraInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSynentraApplication();
        services.AddSynentraConfiguration(configuration);
        services.AddSynentraPersistence();
        services.AddLogging();

        using var scope = services.BuildServiceProvider().CreateScope();
        var dataProtectionConfig = scope.ServiceProvider.GetRequiredService<IOptions<DataProtectionConfiguration>>();

        if (string.IsNullOrWhiteSpace(dataProtectionConfig.Value.KeyRingPath))
            throw new InvalidOperationException("A durable data-protection key-ring path is required.");

        //services.AddOptions<InfrastructureOptions>().Bind(configuration.GetSection("Synentra:Infrastructure")).Validate(o => o.InitialTrustScore is >= 0 and <= 100 && o.QuarantineThreshold is >= 0 and <= 100 && o.HttpTimeoutSeconds > 0 && o.MaximumResponseBytes > 0, "Invalid infrastructure options.").ValidateOnStart();

        services.AddDataProtection().SetApplicationName(dataProtectionConfig.Value.ApplicationName 
            ?? "synentra").PersistKeysToFileSystem(new DirectoryInfo(dataProtectionConfig.Value.KeyRingPath));

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

        Plugin("http", PluginFamily.TransportAdapter, typeof(AdmittedSnapshot), typeof(TransportReceipt), "request_response");
        Plugin("mcp", PluginFamily.TransportAdapter, typeof(AdmittedSnapshot), typeof(TransportReceipt), "request_response");
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

        var outboxConfig = scope.ServiceProvider.GetRequiredService<IOptions<OutboxConfiguration>>();

        if (outboxConfig.Value.Enabled == true)
            services.AddHostedService<OutboxWorker>();

        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("synentra-database").AddCheck<ManifestHealthCheck>("synentra-manifest");
        services.AddScoped<IDispatcher, Dispatcher>();
        return services;
    }
}
