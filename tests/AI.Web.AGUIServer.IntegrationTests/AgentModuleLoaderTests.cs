using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Extensions.Configuration;
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
    public void LoadAgentModule_NoConfig_IsNoOp()
    {
        var builder = CreateBuilder(agentModule: null);

        // Should return without throwing.
        builder.LoadAgentModule();
    }

    [TestMethod]
    public void LoadAgentModule_EmptyConfig_IsNoOp()
    {
        var builder = CreateBuilder(agentModule: "");

        builder.LoadAgentModule();
    }

    [TestMethod]
    public void LoadAgentModule_ValidAssembly_CallsRegister()
    {
        // This test assembly contains exactly one non-abstract IAgentModule: StubAgentModule.
        var thisAssemblyName = typeof(StubAgentModule).Assembly.GetName().Name!;
        var builder = CreateBuilder(agentModule: thisAssemblyName);

        builder.LoadAgentModule();

        Assert.IsTrue(StubAgentModule.RegisterCalled,
            "Register should have been invoked on the discovered module.");
    }

    [TestMethod]
    public void LoadAgentModule_UnknownAssembly_ThrowsInvalidOperationException()
    {
        var builder = CreateBuilder(agentModule: "NonExistent.Assembly.Name");

        var ex = Assert.ThrowsException<InvalidOperationException>(
            () => builder.LoadAgentModule());

        StringAssert.Contains(ex.Message, "NonExistent.Assembly.Name");
    }

    [TestMethod]
    public void LoadAgentModule_ZeroImplementations_ThrowsInvalidOperationException()
    {
        // The main AGUIServer assembly has zero IAgentModule implementations.
        var builder = CreateBuilder(agentModule: "AI.Web.AGUIServer");

        var ex = Assert.ThrowsException<InvalidOperationException>(
            () => builder.LoadAgentModule());

        StringAssert.Contains(ex.Message, "does not contain any implementation");
    }

    [TestMethod]
    public void LoadAgentModule_AmbiguousAssembly_ThrowsInvalidOperationException()
    {
        // Build a dynamic assembly containing two IAgentModule implementations.
        const string asmName = "AmbiguousAgentModuleTestAssembly";
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName(asmName), AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("Main");

        DefineAgentModuleType(moduleBuilder, "ModuleA");
        DefineAgentModuleType(moduleBuilder, "ModuleB");

        var builder = CreateBuilder(agentModule: asmName);

        var ex = Assert.ThrowsException<InvalidOperationException>(
            () => builder.LoadAgentModule());

        StringAssert.Contains(ex.Message, "multiple IAgentModule implementations");
    }

    // --- helpers ---

    private static HostApplicationBuilder CreateBuilder(string? agentModule)
    {
        var builder = Host.CreateApplicationBuilder();

        if (agentModule is not null)
        {
            builder.Configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["AgentModule"] = agentModule });
        }

        return builder;
    }

    /// <summary>
    /// Emits a concrete type that implements <see cref="IAgentModule"/> with a
    /// no-op <c>Register</c> method into the given dynamic module.
    /// </summary>
    private static void DefineAgentModuleType(ModuleBuilder moduleBuilder, string typeName)
    {
        var typeBuilder = moduleBuilder.DefineType(
            typeName, TypeAttributes.Public | TypeAttributes.Class);
        typeBuilder.AddInterfaceImplementation(typeof(IAgentModule));

        var registerMethod = typeBuilder.DefineMethod(
            nameof(IAgentModule.Register),
            MethodAttributes.Public | MethodAttributes.Virtual,
            typeof(void),
            [typeof(IHostApplicationBuilder)]);

        var il = registerMethod.GetILGenerator();
        il.Emit(OpCodes.Ret);

        typeBuilder.DefineMethodOverride(
            registerMethod,
            typeof(IAgentModule).GetMethod(nameof(IAgentModule.Register))!);

        typeBuilder.CreateType();
    }
}
