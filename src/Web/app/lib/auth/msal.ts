import { ConfidentialClientApplication, type Configuration } from "@azure/msal-node";
import { getRedirectUri } from "./config";

let msalInstance: ConfidentialClientApplication | null = null;

function getMsalConfig(): Configuration {
  const clientId = process.env.ENTRA_CLIENT_ID || process.env.AZURE_CLIENT_ID;
  const clientSecret = process.env.ENTRA_CLIENT_SECRET || process.env.AZURE_CLIENT_SECRET;
  const tenantId = process.env.ENTRA_TENANT_ID || process.env.AZURE_TENANT_ID;

  if (!clientId || !clientSecret || !tenantId) {
    throw new Error(
      "ENTRA_CLIENT_ID, ENTRA_CLIENT_SECRET, and ENTRA_TENANT_ID " +
      "are required for local auth mode. AZURE_* is still accepted as a compatibility fallback. " +
      "See docs/LOCAL_AUTH.md."
    );
  }

  return {
    auth: {
      clientId,
      clientSecret,
      authority: `https://login.microsoftonline.com/${tenantId}`,
    },
  };
}

/**
 * Returns a singleton MSAL ConfidentialClientApplication.
 */
export function getMsalClient(): ConfidentialClientApplication {
  if (!msalInstance) {
    msalInstance = new ConfidentialClientApplication(getMsalConfig());
  }
  return msalInstance;
}

/**
 * Default scopes requested during the authorization-code flow.
 * `openid` and `profile` give us the ID token with user claims.
 * `User.Read` allows the backend to call Microsoft Graph for enrichment.
 */
export const DEFAULT_SCOPES = ["openid", "profile", "email", "User.Read"];

/**
 * Generates the Entra ID authorization URL for the sign-in redirect.
 */
export async function getAuthCodeUrl(state?: string): Promise<string> {
  const client = getMsalClient();
  return client.getAuthCodeUrl({
    redirectUri: getRedirectUri(),
    scopes: DEFAULT_SCOPES,
    state,
  });
}

/**
 * Exchanges an authorization code for tokens.
 */
export async function acquireTokenByCode(code: string) {
  const client = getMsalClient();
  return client.acquireTokenByCode({
    code,
    redirectUri: getRedirectUri(),
    scopes: DEFAULT_SCOPES,
  });
}
