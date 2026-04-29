using AI.Agents.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace AI.Agents.Microsoft.Authentication;

/// <summary>
/// Implementation of <see cref="IUserProfileService"/> using the Microsoft Graph SDK.
/// </summary>
public sealed class GraphUserProfileService : IUserProfileService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GraphUserProfileService> _logger;

    public GraphUserProfileService(IHttpClientFactory httpClientFactory, ILogger<GraphUserProfileService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IUserContext?> GetCurrentUserProfileAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var authProvider = new BaseBearerTokenAuthenticationProvider(new StaticAccessTokenProvider(accessToken));
        var httpClient = _httpClientFactory.CreateClient("MicrosoftGraph");
        var requestAdapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient);
        var serviceClient = new GraphServiceClient(requestAdapter);
        var user = await serviceClient.Me.GetAsync(requestConfiguration => { requestConfiguration.QueryParameters.Select = ["id", "displayName", "mail", "userPrincipalName", "givenName", "surname"]; }, cancellationToken);
        if (user is null) return null;
        var picture = await GetCurrentUserPhotoAsDataUrlAsync(serviceClient, accessToken, cancellationToken);
        return new GraphUserContext(
            userId: user.Id!,
            displayName: user.DisplayName,
            email: user.Mail,
            picture: picture,
            accessToken: accessToken);
    }

    internal async Task<string?> GetCurrentUserPhotoAsDataUrlAsync(GraphServiceClient client, string accessToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }
        try
        {
            await using var photoStream = await client.Me.Photo.Content.GetAsync(cancellationToken: cancellationToken);
            if (photoStream is null)
            {
                return null;
            }

            using var memoryStream = new MemoryStream();
            await photoStream.CopyToAsync(memoryStream, cancellationToken);
            var photoBytes = memoryStream.ToArray();

            if (photoBytes.Length == 0)
            {
                return null;
            }

            var mimeType = DetectImageMimeType(photoBytes);
            var base64 = Convert.ToBase64String(photoBytes);

            return $"data:{mimeType};base64,{base64}";
        }
        catch (global::Microsoft.Graph.Models.ODataErrors.ODataError ex) when (ex.ResponseStatusCode == 404)
        {
            _logger.LogDebug("User has no photo set in Microsoft Graph.");
            return null;
        }
        catch (global::Microsoft.Graph.Models.ODataErrors.ODataError ex) when (ex.ResponseStatusCode == 401 || ex.ResponseStatusCode == 403)
        {
            _logger.LogWarning(ex, "Graph access denied fetching photo (HTTP {StatusCode}). The app registration is missing the Microsoft Graph User.Read delegated permission or admin consent has not been granted.", ex.ResponseStatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve Graph photo.");
            return null;
        }
    }

    private static string DetectImageMimeType(byte[] imageBytes)
    {
        if (imageBytes.Length >= 3 && imageBytes[0] == 0xFF && imageBytes[1] == 0xD8 && imageBytes[2] == 0xFF)
        {
            return "image/jpeg";
        }
        if (imageBytes.Length >= 8 && imageBytes[0] == 0x89 && imageBytes[1] == 0x50 && imageBytes[2] == 0x4E && imageBytes[3] == 0x47)
        {
            return "image/png";
        }
        if (imageBytes.Length >= 6 && imageBytes[0] == 0x47 && imageBytes[1] == 0x49 && imageBytes[2] == 0x46)
        {
            return "image/gif";
        }
        if (imageBytes.Length >= 4 && imageBytes[0] == 0x52 && imageBytes[1] == 0x49 && imageBytes[2] == 0x46 && imageBytes[3] == 0x46)
        {
            return "image/webp";
        }

        return "image/jpeg";
    }
}

/// <summary>
/// Simple authentication provider that uses a static access token for all requests.
/// </summary>
internal sealed class StaticAccessTokenProvider : IAccessTokenProvider
{
    private readonly string _accessToken;

    public StaticAccessTokenProvider(string accessToken)
    {
        _accessToken = accessToken;
    }

    public AllowedHostsValidator AllowedHostsValidator { get; } = new AllowedHostsValidator(["graph.microsoft.com"]);

    public Task<string> GetAuthorizationTokenAsync(
        Uri uri,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_accessToken);
    }
}
