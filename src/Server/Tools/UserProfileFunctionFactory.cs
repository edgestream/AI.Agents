using System.ComponentModel;
using System.Text.Json;
using AI.Agents.Abstractions;
using Microsoft.Extensions.AI;

public class UserProfileFunctionFactory
{
    private class UserProfileTool(IUserContextAccessor userContextAccessor)
    {
        /// <summary>
        /// Returns the current authenticated user's profile.
        /// </summary>
        [Description("Returns the current authenticated user's profile.")]
        public JsonElement GetUserProfile()
        {
            var userContext = userContextAccessor.UserContext;
            return JsonSerializer.SerializeToElement(new
            {
                userId = userContext.UserId,
                displayName = userContext.DisplayName,
                email = userContext.Email,
                isAuthenticated = userContext.IsAuthenticated
            });
        }
    }

    /// <summary>
    /// Creates an AI function that returns the current authenticated user's profile. The function uses the IUserContextAccessor service to access user information from the execution context. This can be used by agents to provide personalized responses based on the user's identity and profile details.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve dependencies.</param>
    /// <returns>An AI function that returns the current authenticated user's profile.</returns>
    public static AIFunction CreateAIFunction(IServiceProvider serviceProvider)
    {
        var userContextAccessor = serviceProvider.GetRequiredService<IUserContextAccessor>();
        var userProfileTool = new UserProfileTool(userContextAccessor);
        return AIFunctionFactory.Create(
            userProfileTool.GetUserProfile,
            name: "get_user_profile",
            description: "Returns the current authenticated user's profile.");
    }
}