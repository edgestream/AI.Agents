namespace AI.Agents.Microsoft.Authentication;

/// <summary>
/// Constants for the custom Entra Easy Auth authentication scheme.
/// </summary>
public static class EntraAuthenticationDefaults
{
    public const string AuthenticationScheme = "EntraEasyAuth";
    public const string AccessTokenHeader = "X-MS-TOKEN-AAD-ACCESS-TOKEN";
    public const string PrincipalIdHeader = "X-MS-CLIENT-PRINCIPAL-ID";
    public const string UserContextItemKey = "EntraUserContext";
    public const string UserIdClaimType = "entra_user_id";
    public const string AccessTokenClaimType = "entra_access_token";
    public const string PictureClaimType = "entra_picture";
}
