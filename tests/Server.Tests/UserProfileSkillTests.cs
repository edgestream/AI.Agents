using System.Text.Json;
using AI.Agents.Abstractions;
using AI.Agents.Microsoft.Auth;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace AI.Agents.Server.Tests;

/// <summary>
/// Unit tests for the user profile AI function.
/// </summary>
[TestClass]
public sealed class UserProfileSkillTests
{
    [TestMethod]
    public async Task GetUserProfile_ReturnsDisplayName_ForAuthenticatedUser()
    {
        var function = CreateFunction(
            new GraphUserContext(
                userId: "user-123",
                displayName: "Mario Rossi",
                email: "mario@example.com",
                picture: "data:image/jpeg;base64,large-payload",
                accessToken: "secret-token"));

        var result = await function.InvokeAsync(new AIFunctionArguments());

        var json = ParseJsonResult(result);
        Assert.AreEqual("user-123", json.RootElement.GetProperty("userId").GetString());
        Assert.AreEqual("Mario Rossi", json.RootElement.GetProperty("displayName").GetString());
        Assert.AreEqual("mario@example.com", json.RootElement.GetProperty("email").GetString());
        Assert.IsTrue(json.RootElement.GetProperty("isAuthenticated").GetBoolean());
        Assert.IsFalse(json.RootElement.TryGetProperty("picture", out _));
        Assert.IsFalse(json.RootElement.TryGetProperty("accessToken", out _));
    }

    [TestMethod]
    public async Task GetUserProfile_ReturnsUnavailable_WhenUserIsAnonymous()
    {
        var function = CreateFunction(UnauthenticatedUserContext.Anonymous);

        var result = await function.InvokeAsync(new AIFunctionArguments());

        var json = ParseJsonResult(result);
        Assert.AreEqual(string.Empty, json.RootElement.GetProperty("userId").GetString());
        Assert.AreEqual(JsonValueKind.Null, json.RootElement.GetProperty("displayName").ValueKind);
        Assert.IsFalse(json.RootElement.GetProperty("isAuthenticated").GetBoolean());
        Assert.IsFalse(json.RootElement.TryGetProperty("picture", out _));
        Assert.IsFalse(json.RootElement.TryGetProperty("accessToken", out _));
    }

    [TestMethod]
    public void Create_HasExpectedDeclaration()
    {
        var function = CreateFunction(UnauthenticatedUserContext.Anonymous);

        Assert.AreEqual("get_user_profile", function.Name);
        Assert.IsNotNull(function.Description);
        Assert.IsTrue(function.Description.Contains("user", StringComparison.OrdinalIgnoreCase));
    }

    private static AIFunction CreateFunction(IUserContext userContext)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUserContextAccessor>(new TestUserContextAccessor(userContext));
        return global::UserProfileFunctionFactory.Create(services.BuildServiceProvider());
    }

    private static JsonDocument ParseJsonResult(object? result)
    {
        return result switch
        {
            JsonElement element => JsonDocument.Parse(element.GetRawText()),
            string json => JsonDocument.Parse(json),
            _ => JsonDocument.Parse(JsonSerializer.Serialize(result)),
        };
    }

    private sealed class TestUserContextAccessor(IUserContext userContext) : IUserContextAccessor
    {
        public IUserContext UserContext { get; } = userContext;
    }
}
