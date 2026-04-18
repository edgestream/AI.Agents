using AI.Agents.Abstractions;
using System.ComponentModel;
using System.Text.Json;
using Microsoft.Agents.AI;

namespace AI.Agents.Server;

#pragma warning disable MAAI001 // AgentClassSkill is marked experimental

/// <summary>
/// A class-based agent skill for accessing the current authenticated user's profile.
/// </summary>
public sealed class UserProfileSkill(IUserContextAccessor userContextAccessor) : AgentClassSkill<UserProfileSkill>
{
    /// <inheritdoc />
    public override AgentSkillFrontmatter Frontmatter { get; } = new(
        "user-profile",
        "Get details about the current authenticated user. Use when the user asks for their own name, e-mail address or other user profile details."
    );

    /// <inheritdoc />
    protected override string Instructions => """
        Use this skill when the user asks for their own name, e-mail address or other user profile details.
        Use the get_user_profile script to retrieve the current authenticated user's display name.
        If no authenticated user is present, explain that no signed-in user information is available.
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
            userId = userContext.UserId,
            displayName = userContext.DisplayName,
            email = userContext.Email,
            isAuthenticated = userContext.IsAuthenticated
        });
    }
}

#pragma warning restore MAAI001
