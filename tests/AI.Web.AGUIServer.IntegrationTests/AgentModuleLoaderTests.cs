using AI.AGUI.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AI.Web.AGUIServer.IntegrationTests;

#region Test doubles

/// <summary>Single valid <see cref="IAgentModule"/> used by the loader tests.</summary>
public sealed class StubAgentModule : IAgentModule
{
    public static bool RegisterCalled { get; set; }

    public void Register(IHostApplicationBuilder builder)
    {
        RegisterCalled = true;
    }
}

#endregion

[TestClass]
public sealed class AgentModuleLoaderTests
{
    [TestInitialize]
    public void ResetState()
    {
        StubAgentModule.RegisterCalled = false;
    }

    [TestMethod]
    public void LoadAgentModule_PreRegisteredModule_CallsRegister()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<IAgentModule>(new StubAgentModule());

        builder.LoadAgentModule();

        Assert.IsTrue(StubAgentModule.RegisterCalled,
            "Register should have been invoked on the pre-registered module.");
    }

    [TestMethod]
    public void LoadAgentModule_PreRegisteredModule_ReturnsTrue()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<IAgentModule>(new StubAgentModule());

        bool result = builder.LoadAgentModule();

        Assert.IsTrue(result);
    }
}
