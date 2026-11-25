using System.Text;
using FluentAssertions;
using NSubstitute;
using Synentra.Application.Common.Dispatching;
using Synentra.Application.Common.Plugins;
using Synentra.Application.Features.Manifests;
using Synentra.Application.Features.Manifests.Commands.ActivateManifest;
using Synentra.Application.Features.Manifests.Commands.StoreManifest;
using Synentra.Application.Features.Manifests.Contracts;
using Synentra.Application.Features.Manifests.Exceptions;
using Synentra.Application.Features.Manifests.Models;
using Synentra.Application.Features.Manifests.Queries.GetActiveManifestRevision;
using Synentra.Application.Features.Manifests.Queries.ValidateManifest;
using Synentra.Application.Tests.TestHelpers;

namespace Synentra.Application.Tests.Features.Manifests;

public sealed class ManifestTests
{
    private readonly IPluginResolver _plugins = Substitute.For<IPluginResolver>();
    private readonly IBundleParser _parser = Substitute.For<IBundleParser>();
    private readonly IManifestStore _store = Substitute.For<IManifestStore>();
    private readonly IManifestProviderInternal _providerInternal = Substitute.For<IManifestProviderInternal>();

    public ManifestTests()
    {
        _plugins.HasRegistration(Arg.Any<string?>()).Returns(true);
    }

    [Fact]
    public void BundleValidator_WhenBundleIsValid_ShouldReturnIsValidTrueWithNoIssues()
    {
        var validator = new BundleValidator(_plugins);
        var bundle = TestDataBuilder.CreateValidBundle();

        var result = validator.Validate(bundle);

        result.IsValid.Should().BeTrue();
        result.Issues.Should().BeEmpty();
    }

    [Fact]
    public void BundleValidator_WhenVersionKindDuplicatesOrUnresolvedRefsExist_ShouldReportSpecificErrors()
    {
        _plugins.HasRegistration("missing-resolver").Returns(false);
        _plugins.HasRegistration("missing-verifier").Returns(false);
        _plugins.HasRegistration("missing-adapter").Returns(false);
        _plugins.HasRegistration("order-resolver").Returns(true);
        _plugins.HasRegistration("order-verifier").Returns(true);
        _plugins.HasRegistration("http-adapter").Returns(true);

        var validator = new BundleValidator(_plugins);
        var bundle = TestDataBuilder.CreateValidBundle();
        bundle.ApiVersion = "synentra.io/v1";
        bundle.Kind = "WrongKind";
        bundle.Spec.Profiles.Add(new ProfileModel { Id = "default-profile" }); // duplicate
        bundle.Spec.Targets.Add(new TargetModel { Id = "bad-target", AdapterRef = "missing-adapter" });
        bundle.Spec.Operations[0].ProfileRef = "unknown-profile";
        bundle.Spec.Operations[0].PolicyRefs.Add("unknown-policy");
        bundle.Spec.Operations[0].Mutability = "invalidMutability";
        bundle.Spec.Operations[0].Execution.Mode = "invalidMode";
        bundle.Spec.Operations[0].Resources.Add(new ResolverModel { Role = "order", ResolverRef = "missing-resolver" }); // dup role + missing resolver
        bundle.Spec.Operations[0].Verification = new VerificationModel { VerifierRef = "missing-verifier" };
        bundle.Spec.OperationBindings[0].CanonicalToTarget!["no-slash"] = new MappingEntryModel(); // missing source + invalid pointer
        bundle.Spec.Policies[0].Rules[0].Effect = "invalidEffect";
        bundle.Spec.Policies[0].Rules[0].Match.ResourceMatches = [new ResourceMatchModel { Quantifier = "invalidQuantifier" }];

        var result = validator.Validate(bundle);

        result.IsValid.Should().BeFalse();
        var codes = result.Issues.Select(i => i.Code).ToList();
        codes.Should().Contain(new[]
        {
            "UNSUPPORTED_API_VERSION",
            "INVALID_KIND",
            "DUPLICATE_PROFILE_ID",
            "UNRESOLVED_ADAPTER_REF",
            "UNRESOLVED_PROFILE_REF",
            "UNRESOLVED_POLICY_REF",
            "INVALID_MUTABILITY",
            "INVALID_EXECUTION_MODEL",
            "UNRESOLVED_RESOURCE_RESOLVER",
            "UNRESOLVED_VERIFIER",
            "DUPLICATE_RESOURCE_ROLE",
            "MAPPING_MISSING_SOURCE",
            "MAPPING_INVALID_POINTER",
            "INVALID_POLICY_EFFECT",
            "INVALID_QUANTIFIER"
        });
    }

    [Fact]
    public async Task StoreManifestCommandHandler_WhenValid_ShouldHashAndStoreVersion()
    {
        var validator = new BundleValidator(_plugins);
        var bundle = TestDataBuilder.CreateValidBundle("my-manifest", "2.0.0");
        _parser.Parse(Arg.Any<Stream>()).Returns(bundle);
        _store.StoreVersionAsync("my-manifest", "2.0.0", "{\"valid\":true}", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("rev-2");

        var handler = new StoreManifestCommandHandler(_parser, validator, _store);
        var rev = await handler.Handle(new StoreManifestCommand("{\"valid\":true}"), TestContext.Current.CancellationToken);

        rev.Should().Be("rev-2");
    }

    [Fact]
    public async Task StoreManifestCommandHandler_WhenInvalidBundle_ShouldThrowInvalidOperationException()
    {
        var validator = new BundleValidator(_plugins);
        var invalidBundle = TestDataBuilder.CreateValidBundle();
        invalidBundle.Kind = "Invalid";
        _parser.Parse(Arg.Any<Stream>()).Returns(invalidBundle);

        var handler = new StoreManifestCommandHandler(_parser, validator, _store);
        var act = () => handler.Handle(new StoreManifestCommand("{}"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*invalid bundle*");
    }

    [Fact]
    public async Task ActivateManifestCommandHandler_WhenExpectedRevisionMatches_ShouldActivateAtomically()
    {
        var validator = new BundleValidator(_plugins);
        var bundle = TestDataBuilder.CreateValidBundle();
        _store.GetActiveRevisionAsync(Arg.Any<CancellationToken>()).Returns("rev-1");
        _store.GetVersionByVersionAsync("orders-bundle", "2.0.0", Arg.Any<CancellationToken>())
            .Returns(new ManifestVersionSnapshot("orders-bundle", "rev-2", "{}"));
        _parser.Parse(Arg.Any<Stream>()).Returns(bundle);
        _store.ActivateAtomicallyAsync("orders-bundle", "rev-2", "rev-1", Arg.Any<CancellationToken>())
            .Returns("rev-2");

        var handler = new ActivateManifestCommandHandler(_parser, validator, _store, _providerInternal);
        var result = await handler.Handle(new ActivateManifestCommand("orders-bundle", "2.0.0", "rev-1"), TestContext.Current.CancellationToken);

        result.Activated.Should().BeTrue();
        result.PreviousRevision.Should().Be("rev-1");
        result.NewRevision.Should().Be("rev-2");
        _providerInternal.Received(1).SetActive(bundle, "rev-2");
    }

    [Fact]
    public async Task ActivateManifestCommandHandler_WhenExpectedRevisionMismatches_ShouldThrowActivationConflictException()
    {
        var validator = new BundleValidator(_plugins);
        _store.GetActiveRevisionAsync(Arg.Any<CancellationToken>()).Returns("rev-current");

        var handler = new ActivateManifestCommandHandler(_parser, validator, _store, _providerInternal);
        var act = () => handler.Handle(new ActivateManifestCommand("orders-bundle", "2.0.0", "rev-stale"));

        var ex = await act.Should().ThrowAsync<ActivationConflictException>();
        ex.Which.CurrentRevision.Should().Be("rev-current");
        ex.Which.ExpectedRevision.Should().Be("rev-stale");
    }

    [Fact]
    public async Task ValidateManifestQueryHandler_WhenParserThrowsBundleParseException_ShouldReturnParseErrorIssues()
    {
        var validator = new BundleValidator(_plugins);
        _parser.Parse(Arg.Any<Stream>()).Returns(_ => throw new BundleParseException("Bad JSON", new[] { "Unexpected token" }));

        var handler = new ValidateManifestQueryHandler(_parser, validator);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("bad"));
        var result = await handler.Handle(new ValidateManifestQuery(stream), TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Issues.Should().ContainSingle(i => i.Code == "PARSE_ERROR" && i.Message == "Unexpected token");
    }

    [Fact]
    public async Task GetActiveRevisionAndManagerAndExtensionsAndValidators_ShouldWork()
    {
        _store.GetActiveRevisionAsync(Arg.Any<CancellationToken>()).Returns("rev-active");
        var getRevHandler = new GetActiveManifestRevisionQueryHandler(_store);
        (await getRevHandler.Handle(new GetActiveManifestRevisionQuery(), TestContext.Current.CancellationToken)).Should().Be("rev-active");

        new ActivateManifestCommandValidator().Validate(new ActivateManifestCommand("", "r2", null)).IsValid.Should().BeFalse();
        new ActivateManifestCommandValidator().Validate(new ActivateManifestCommand("m1", "", null)).IsValid.Should().BeFalse();
        new ActivateManifestCommandValidator().Validate(new ActivateManifestCommand("m1", "r2", null)).IsValid.Should().BeTrue();

        new StoreManifestCommandValidator().Validate(new StoreManifestCommand("")).IsValid.Should().BeFalse();
        new StoreManifestCommandValidator().Validate(new StoreManifestCommand("{}")).IsValid.Should().BeTrue();

        new ValidateManifestQueryValidator().Validate(new ValidateManifestQuery(null!)).IsValid.Should().BeFalse();
        using var ms = new MemoryStream();
        new ValidateManifestQueryValidator().Validate(new ValidateManifestQuery(ms)).IsValid.Should().BeTrue();

        var retention = new ResponseRetentionModel { Mode = "invalid" };
        FluentActions.Invoking(() => retention.Validate()).Should().Throw<InvalidOperationException>();

        var dispatcher = Substitute.For<IDispatcher>();
        var manager = new ManifestManager(dispatcher);
        await manager.ValidateAsync(ms, TestContext.Current.CancellationToken);
        await manager.StoreAsync("{}", TestContext.Current.CancellationToken);
        await manager.ActivateAsync("m1", "r2", "r1", TestContext.Current.CancellationToken);
        await manager.GetActiveRevisionAsync(TestContext.Current.CancellationToken);

        await dispatcher.ValidateManifest(ms, TestContext.Current.CancellationToken);
        await dispatcher.StoreManifest("{}", TestContext.Current.CancellationToken);
        await dispatcher.Activate("m1", "r2", "r1", TestContext.Current.CancellationToken);
    }
}
