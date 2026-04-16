using System.Text.Json;
using AI.AGUI.Auth;
using AI.AGUI.Server;

namespace AI.AGUI.Server.IntegrationTests;

/// <summary>
/// Unit tests for <see cref="UserProfileSkill"/>.
/// </summary>
[TestClass]
public sealed class UserProfileSkillTests
{
    [TestMethod]
    public void GetUserFullName_ReturnsDisplayName_ForAuthenticatedUser()
    {
        var skill = new UserProfileSkill(new TestUserContextAccessor(
            new UserContext(
                userId: "user-123",
                displayName: "Mario Rossi",
                email: "mario@example.com")));

        var result = skill.GetUserFullName();

        var json = JsonDocument.Parse(result);
        Assert.IsTrue(json.RootElement.GetProperty("available").GetBoolean());
        Assert.AreEqual("Mario Rossi", json.RootElement.GetProperty("fullName").GetString());
        Assert.AreEqual("mario@example.com", json.RootElement.GetProperty("email").GetString());
        Assert.AreEqual("user-123", json.RootElement.GetProperty("userId").GetString());
        Assert.IsTrue(json.RootElement.GetProperty("isAuthenticated").GetBoolean());
    }

    [TestMethod]
    public void GetUserFullName_ReturnsUnavailable_WhenUserIsAnonymous()
    {
        var skill = new UserProfileSkill(new TestUserContextAccessor(UserContext.Anonymous));

        var result = skill.GetUserFullName();

        var json = JsonDocument.Parse(result);
        Assert.IsFalse(json.RootElement.GetProperty("available").GetBoolean());
        Assert.AreEqual(JsonValueKind.Null, json.RootElement.GetProperty("fullName").ValueKind);
        Assert.IsFalse(json.RootElement.GetProperty("isAuthenticated").GetBoolean());
    }

    [TestMethod]
    public void Frontmatter_HasExpectedValues()
    {
        var skill = new UserProfileSkill(new TestUserContextAccessor(UserContext.Anonymous));

        Assert.AreEqual("user-profile", skill.Frontmatter.Name);
        Assert.IsTrue(skill.Frontmatter.Description.Contains("user", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class TestUserContextAccessor(IUserContext userContext) : IUserContextAccessor
    {
        public IUserContext UserContext { get; } = userContext;
    }
}
