using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using FluentAssertions;
using Stl.Collections;
using Stl.DependencyInjection;
using Stl.Plugins;

namespace Stl.Tests.Plugins
{
    public interface ITestPlugin
    {
        string GetName();
    }

    public interface ITestSingletonPlugin : ITestPlugin, ISingletonPlugin
    {
    }

    public interface ITestPluginEx : ITestPlugin
    {
        string GetVersion();
    }

    public abstract class TestPlugin : ITestPlugin
    {
        public virtual string GetName() => GetType().Name;
    }

    [Plugin]
    public class TestPlugin1 : TestPlugin, IHasDependencies, ITestSingletonPlugin
    {
        public IEnumerable<Type> Dependencies { get; } = new [] { typeof(TestPlugin2) };

        public TestPlugin1(IPluginInfoProvider _) { }
        [ServiceConstructor]
        public TestPlugin1() { }
    }

    [Plugin]
    public class TestPlugin2 : TestPlugin, ITestPluginEx, IHasCapabilities, ITestSingletonPlugin
    {
        public virtual string GetVersion() => "1.0";
        public ImmutableOptionSet Capabilities { get; } = ImmutableOptionSet.Empty
            .Set("Client", true)
            .Set("Server", false);

        public TestPlugin2(IPluginInfoProvider _) { }

        [ServiceConstructor]
        public TestPlugin2(IServiceProvider services)
        {
            services.Should().NotBeNull();
        }
    }

    [Plugin]
    public class WrongPlugin : IHasDependencies
    {
        public IEnumerable<Type> Dependencies { get; } = new [] { typeof(TestPlugin2) };

        public WrongPlugin(IPluginInfoProvider _) { }
        [ServiceConstructor]
        public WrongPlugin() { }
    }
}
