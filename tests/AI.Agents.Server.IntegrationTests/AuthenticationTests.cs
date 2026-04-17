using AI.Agents.MCP;
using AI.Agents.Microsoft.Auth;
using AI.Agents.OAuth;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AI.Agents.Server.IntegrationTests;

[TestClass]
public sealed class AuthenticationTests
{
    [TestMethod]
    public void UserContext_Authenticated_HasProperties()
    {
        var context = new UserContext(
            userId: "user-123",
            displayName: "Test User",
            email: "test@example.com",
            picture: "https://example.com/avatar.png",
            accessToken: "test-token");

        Assert.IsTrue(context.IsAuthenticated);
        Assert.AreEqual("user-123", context.UserId);
        Assert.AreEqual("Test User", context.DisplayName);
        Assert.AreEqual("test@example.com", context.Email);
        Assert.AreEqual("https://example.com/avatar.png", context.Picture);
        Assert.AreEqual("test-token", context.AccessToken);
    }

    [TestMethod]
    public void UserContext_Anonymous_IsNotAuthenticated()
    {
        var context = UserContext.Anonymous;

        Assert.IsFalse(context.IsAuthenticated);
        Assert.AreEqual(string.Empty, context.UserId);
        Assert.IsNull(context.DisplayName);
        Assert.IsNull(context.Email);
        Assert.IsNull(context.AccessToken);
    }

    [TestMethod]
    public async Task InMemoryOAuthTokenStore_SetAndGet_ReturnsToken()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new OAuthTokenStoreOptions());
        var store = new InMemoryOAuthTokenStore(cache, options);

        var token = new OAuthToken
        {
            AccessToken = "test-access-token",
            RefreshToken = "test-refresh-token",
            TokenType = "Bearer",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            Scopes = ["repo", "read:user"]
        };

        await store.SetTokenAsync("user-1", "github", token);
        var retrieved = await store.GetTokenAsync("user-1", "github");

        Assert.IsNotNull(retrieved);
        Assert.AreEqual("test-access-token", retrieved.AccessToken);
        Assert.AreEqual("test-refresh-token", retrieved.RefreshToken);
        Assert.AreEqual("Bearer", retrieved.TokenType);
        Assert.AreEqual(2, retrieved.Scopes.Count);
    }

    [TestMethod]
    public async Task InMemoryOAuthTokenStore_GetNonExistent_ReturnsNull()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new OAuthTokenStoreOptions());
        var store = new InMemoryOAuthTokenStore(cache, options);

        var retrieved = await store.GetTokenAsync("user-1", "nonexistent");

        Assert.IsNull(retrieved);
    }

    [TestMethod]
    public async Task InMemoryOAuthTokenStore_Remove_DeletesToken()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new OAuthTokenStoreOptions());
        var store = new InMemoryOAuthTokenStore(cache, options);

        var token = new OAuthToken
        {
            AccessToken = "test-access-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };

        await store.SetTokenAsync("user-1", "github", token);
        await store.RemoveTokenAsync("user-1", "github");
        var retrieved = await store.GetTokenAsync("user-1", "github");

        Assert.IsNull(retrieved);
    }

    [TestMethod]
    public async Task InMemoryOAuthTokenStore_HasValidToken_TrueForValidToken()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new OAuthTokenStoreOptions());
        var store = new InMemoryOAuthTokenStore(cache, options);

        var token = new OAuthToken
        {
            AccessToken = "test-access-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };

        await store.SetTokenAsync("user-1", "github", token);
        var hasValid = await store.HasValidTokenAsync("user-1", "github");

        Assert.IsTrue(hasValid);
    }

    [TestMethod]
    public async Task InMemoryOAuthTokenStore_HasValidToken_FalseForExpiredToken()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new OAuthTokenStoreOptions());
        var store = new InMemoryOAuthTokenStore(cache, options);

        var token = new OAuthToken
        {
            AccessToken = "test-access-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1) // Already expired
        };

        await store.SetTokenAsync("user-1", "github", token);
        var hasValid = await store.HasValidTokenAsync("user-1", "github");

        Assert.IsFalse(hasValid);
    }

    [TestMethod]
    public void OAuthToken_IsExpired_TrueWhenExpired()
    {
        var token = new OAuthToken
        {
            AccessToken = "test",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        };

        Assert.IsTrue(token.IsExpired);
    }

    [TestMethod]
    public void OAuthToken_IsExpired_TrueWhenAboutToExpire()
    {
        var token = new OAuthToken
        {
            AccessToken = "test",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(2) // Within 5 minute buffer
        };

        Assert.IsTrue(token.IsExpired);
    }

    [TestMethod]
    public void OAuthToken_IsExpired_FalseWhenValid()
    {
        var token = new OAuthToken
        {
            AccessToken = "test",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };

        Assert.IsFalse(token.IsExpired);
    }

    [TestMethod]
    public void OAuthState_GenerateStateId_ReturnsUniqueValues()
    {
        var state1 = OAuthState.GenerateStateId();
        var state2 = OAuthState.GenerateStateId();

        Assert.AreNotEqual(state1, state2);
        Assert.IsTrue(state1.Length > 0);
        Assert.IsTrue(state2.Length > 0);
    }

    [TestMethod]
    public void OAuthState_CodeChallenge_ComputesCorrectly()
    {
        var verifier = OAuthState.GenerateCodeVerifier();
        var challenge = OAuthState.ComputeCodeChallenge(verifier);

        Assert.AreNotEqual(verifier, challenge);
        Assert.IsTrue(challenge.Length > 0);
        
        // Same verifier should produce same challenge
        var challenge2 = OAuthState.ComputeCodeChallenge(verifier);
        Assert.AreEqual(challenge, challenge2);
    }

    [TestMethod]
    public async Task InMemoryOAuthStateStore_StoreAndConsume_ReturnsState()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var store = new InMemoryOAuthStateStore(cache);

        var state = new OAuthState
        {
            StateId = "test-state-id",
            UserId = "user-123",
            McpServerName = "github",
            CodeVerifier = "test-verifier"
        };

        await store.StoreStateAsync(state);
        var retrieved = await store.ConsumeStateAsync("test-state-id");

        Assert.IsNotNull(retrieved);
        Assert.AreEqual("user-123", retrieved.UserId);
        Assert.AreEqual("github", retrieved.McpServerName);
        Assert.AreEqual("test-verifier", retrieved.CodeVerifier);
    }

    [TestMethod]
    public async Task InMemoryOAuthStateStore_ConsumeRemovesState()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var store = new InMemoryOAuthStateStore(cache);

        var state = new OAuthState
        {
            StateId = "test-state-id",
            UserId = "user-123",
            McpServerName = "github"
        };

        await store.StoreStateAsync(state);
        await store.ConsumeStateAsync("test-state-id");
        var secondRetrieve = await store.ConsumeStateAsync("test-state-id");

        Assert.IsNull(secondRetrieve);
    }

    [TestMethod]
    public async Task InMemoryOAuthStateStore_ConsumeNonExistent_ReturnsNull()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var store = new InMemoryOAuthStateStore(cache);

        var retrieved = await store.ConsumeStateAsync("nonexistent");

        Assert.IsNull(retrieved);
    }

    [TestMethod]
    public async Task MCPAuthorizationService_NoOAuthConfig_AlwaysAuthorized()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var tokenStore = new InMemoryOAuthTokenStore(cache, Options.Create(new OAuthTokenStoreOptions()));
        var mcpOptions = Options.Create(new MCPClientOptions
        {
            Servers = new Dictionary<string, MCPServerOptions>
            {
                ["filesystem"] = new MCPServerOptions { Type = "stdio", Command = "npx" }
            }
        });
        var service = new MCPAuthorizationService(tokenStore, mcpOptions);

        var isAuthorized = await service.IsAuthorizedAsync("filesystem", "user-123");

        Assert.IsTrue(isAuthorized);
    }

    [TestMethod]
    public async Task MCPAuthorizationService_WithOAuthConfig_NotAuthorizedWithoutToken()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var tokenStore = new InMemoryOAuthTokenStore(cache, Options.Create(new OAuthTokenStoreOptions()));
        var mcpOptions = Options.Create(new MCPClientOptions
        {
            Servers = new Dictionary<string, MCPServerOptions>
            {
                ["github"] = new MCPServerOptions
                {
                    Type = "stdio",
                    Command = "npx",
                    Auth = new MCPOAuthOptions
                    {
                        ClientId = "test-client",
                        AuthorizationUrl = "https://github.com/login/oauth/authorize",
                        TokenUrl = "https://github.com/login/oauth/access_token"
                    }
                }
            }
        });
        var service = new MCPAuthorizationService(tokenStore, mcpOptions);

        var isAuthorized = await service.IsAuthorizedAsync("github", "user-123");

        Assert.IsFalse(isAuthorized);
    }

    [TestMethod]
    public async Task MCPAuthorizationService_WithOAuthConfig_AuthorizedWithValidToken()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var tokenStore = new InMemoryOAuthTokenStore(cache, Options.Create(new OAuthTokenStoreOptions()));
        var mcpOptions = Options.Create(new MCPClientOptions
        {
            Servers = new Dictionary<string, MCPServerOptions>
            {
                ["github"] = new MCPServerOptions
                {
                    Type = "stdio",
                    Command = "npx",
                    Auth = new MCPOAuthOptions
                    {
                        ClientId = "test-client",
                        AuthorizationUrl = "https://github.com/login/oauth/authorize",
                        TokenUrl = "https://github.com/login/oauth/access_token"
                    }
                }
            }
        });
        var service = new MCPAuthorizationService(tokenStore, mcpOptions);

        // Store a valid token
        await tokenStore.SetTokenAsync("user-123", "github", new OAuthToken
        {
            AccessToken = "valid-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        });

        var isAuthorized = await service.IsAuthorizedAsync("github", "user-123");

        Assert.IsTrue(isAuthorized);
    }

    [TestMethod]
    public void MCPAuthorizationService_GenerateConsentRequired_ReturnsConsentInfo()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var tokenStore = new InMemoryOAuthTokenStore(cache, Options.Create(new OAuthTokenStoreOptions()));
        var mcpOptions = Options.Create(new MCPClientOptions
        {
            Servers = new Dictionary<string, MCPServerOptions>
            {
                ["github"] = new MCPServerOptions
                {
                    Type = "stdio",
                    Command = "npx",
                    Auth = new MCPOAuthOptions
                    {
                        ClientId = "test-client",
                        AuthorizationUrl = "https://github.com/login/oauth/authorize",
                        TokenUrl = "https://github.com/login/oauth/access_token",
                        Scopes = ["repo", "read:user"]
                    }
                }
            }
        });
        var service = new MCPAuthorizationService(tokenStore, mcpOptions);

        var consent = service.GenerateConsentRequired("github", "https://example.com");

        Assert.IsNotNull(consent);
        Assert.AreEqual("github", consent.McpServerName);
        Assert.AreEqual("GitHub", consent.DisplayName);
        Assert.AreEqual("https://example.com/oauth/authorize/github", consent.AuthorizeUrl);
        Assert.AreEqual(2, consent.Scopes.Count);
    }

    [TestMethod]
    public void MCPAuthorizationService_GenerateConsentRequired_NoOAuthConfig_ReturnsNull()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var tokenStore = new InMemoryOAuthTokenStore(cache, Options.Create(new OAuthTokenStoreOptions()));
        var mcpOptions = Options.Create(new MCPClientOptions
        {
            Servers = new Dictionary<string, MCPServerOptions>
            {
                ["filesystem"] = new MCPServerOptions { Type = "stdio", Command = "npx" }
            }
        });
        var service = new MCPAuthorizationService(tokenStore, mcpOptions);

        var consent = service.GenerateConsentRequired("filesystem", "https://example.com");

        Assert.IsNull(consent);
    }
}
