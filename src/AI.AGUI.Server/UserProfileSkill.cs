using AI.Agents.Auth;
using System.ComponentModel;
using System.Text.Json;
using Microsoft.Agents.AI;

namespace AI.AGUI.Server;

#pragma warning disable MAAI001 // AgentClassSkill is marked experimental

/// <summary>
/// A class-based agent skill for accessing the current authenticated user's profile.
/// </summary>
public sealed class UserProfileSkill(IUserContextAccessor userContextAccessor) : AgentClassSkill<UserProfileSkill>
{
    /// <inheritdoc />
    public override AgentSkillFrontmatter Frontmatter { get; } = new(
        "user-profile",
        "Get details about the current authenticated user. Use when the user asks for their own name or profile details."
    );

    /// <inheritdoc />
    protected override string Instructions => """
        Use this skill when the user asks for their own name or profile details.
        1. Use the get_user_profile script to retrieve the current authenticated user's display name.
        2. If no authenticated user is present, explain that no signed-in user information is available.
        """;

    /// <summary>
    /// Returns the current authenticated user's profile.
    /// </summary>
    [AgentSkillScript("get_user_profile")]
    [Description("Returns the current authenticated user's profile from the current request identity.")]
    public string GetUserProfile()
    {
        var userContext = userContextAccessor.UserContext;

        return JsonSerializer.Serialize(new
        {
            available = userContext.IsAuthenticated && !string.IsNullOrWhiteSpace(userContext.DisplayName),
            fullName = userContext.DisplayName,
            email = userContext.Email,
            userId = userContext.IsAuthenticated ? userContext.UserId : null,
            isAuthenticated = userContext.IsAuthenticated
        });
    }
}

#pragma warning restore MAAI001
