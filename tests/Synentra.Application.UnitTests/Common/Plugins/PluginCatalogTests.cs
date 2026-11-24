using FluentAssertions;
using NSubstitute;
using Synentra.Application.Common.Plugins;
using Synentra.Diagnostics.Exceptions;

namespace Synentra.Application.UnitTests.Common.Plugins;

public sealed class PluginCatalogTests
{
    [Fact]
    public void Register_and_resolve_returns_registered_plugin()
    {
        var plugin = Substitute.For<IPluginRegistration>();
        plugin.Id.Returns("adapter");
        plugin.Version.Returns("1.0");
        var catalog = new PluginCatalog();

        catalog.Register(plugin);

        catalog.Resolve("adapter", "1.0").Should().BeSameAs(plugin);
        catalog.All.Should().ContainSingle().Which.Should().BeSameAs(plugin);
    }

    [Fact]
    public void Register_rejects_duplicate_id_and_version()
    {
        var plugin = Substitute.For<IPluginRegistration>();
        plugin.Id.Returns("adapter");
        plugin.Version.Returns("1.0");
        var catalog = new PluginCatalog();
        catalog.Register(plugin);

        var action = () => catalog.Register(plugin);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Duplicate plugin registration: adapter@1.0");
    }

    [Fact]
    public void Resolve_throws_for_unknown_plugin()
    {
        var action = () => new PluginCatalog().Resolve("missing", "1.0");

        action.Should().Throw<SynentraApplicationException>();
    }
}
