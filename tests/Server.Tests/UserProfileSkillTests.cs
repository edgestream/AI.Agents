using System.Text.Json;
using AI.Agents.Abstractions;
using AI.Agents.Microsoft.Auth;
using AI.Agents.Server;

namespace AI.Agents.Server.Tests;

/// <summary>
/// Unit tests for <see cref="UserProfileSkill"/>.
/// </summary>
[TestClass]
public sealed class UserProfileSkillTests
{
    [TestMethod]
    public void GetUserProfile_ReturnsDisplayName_ForAuthenticatedUser()
    {
        var skill = new UserProfileSkill(
            new TestUserContextAccessor(
                new GraphUserContext(
                    userId: "user-123",
                    displayName: "Mario Rossi",
                    email: "mario@example.com",
                    picture: "data:image/jpeg;base64,large-payload",
                    accessToken: "secret-token")
                )
            );

        var result = skill.GetUserProfile();

        var json = JsonDocument.Parse(result);
        Assert.AreEqual("user-123", json.RootElement.GetProperty("userId").GetString());
        Assert.AreEqual("Mario Rossi", json.RootElement.GetProperty("displayName").GetString());
        Assert.AreEqual("mario@example.com", json.RootElement.GetProperty("email").GetString());
        Assert.IsTrue(json.RootElement.GetProperty("isAuthenticated").GetBoolean());
        Assert.IsFalse(json.RootElement.TryGetProperty("Picture", out _));
        Assert.IsFalse(json.RootElement.TryGetProperty("AccessToken", out _));
    }

    [TestMethod]
    public void GetUserProfile_ReturnsUnavailable_WhenUserIsAnonymous()
    {
        var skill = new UserProfileSkill(new TestUserContextAccessor(UnauthenticatedUserContext.Anonymous));

        var result = skill.GetUserProfile();

        var json = JsonDocument.Parse(result);
        Assert.AreEqual(string.Empty, json.RootElement.GetProperty("userId").GetString());
        Assert.AreEqual(JsonValueKind.Null, json.RootElement.GetProperty("displayName").ValueKind);
        Assert.IsFalse(json.RootElement.GetProperty("isAuthenticated").GetBoolean());
        Assert.IsFalse(json.RootElement.TryGetProperty("Picture", out _));
        Assert.IsFalse(json.RootElement.TryGetProperty("AccessToken", out _));
    }

    [TestMethod]
    public void Frontmatter_HasExpectedValues()
    {
        var skill = new UserProfileSkill(new TestUserContextAccessor(UnauthenticatedUserContext.Anonymous));

        Assert.AreEqual("user-profile", skill.Frontmatter.Name);
        Assert.IsTrue(skill.Frontmatter.Description.Contains("user", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class TestUserContextAccessor(IUserContext userContext) : IUserContextAccessor
    {
        public IUserContext UserContext { get; } = userContext;
    }
}
