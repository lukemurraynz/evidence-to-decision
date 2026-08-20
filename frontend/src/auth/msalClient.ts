import {
  InteractionRequiredAuthError,
  PublicClientApplication,
  type AccountInfo,
  type Configuration,
} from '@azure/msal-browser'

const RETURN_TO_KEY = 'oe.auth.returnTo'

export class AuthConfigError extends Error {
  public constructor(message: string) {
    super(message)
    this.name = 'AuthConfigError'
  }
}

function requireAuthConfig(
  authClientId: string,
  authTenantId: string,
  authScope: string,
): { readonly clientId: string; readonly tenantId: string; readonly scope: string } {
  if (!authClientId || !authTenantId || !authScope) {
    throw new AuthConfigError(
      'Sign-in is not configured. Ask an administrator to check the site configuration.',
    )
  }
  return { clientId: authClientId, tenantId: authTenantId, scope: authScope }
}

export function createMsalClient(
  authClientId: string,
  authTenantId: string,
  authScope: string,
): { readonly instance: PublicClientApplication; readonly scope: string } {
  const { clientId, tenantId, scope } = requireAuthConfig(
    authClientId,
    authTenantId,
    authScope,
  )
  const config: Configuration = {
    auth: {
      clientId,
      authority: `https://login.microsoftonline.com/${tenantId}`,
      redirectUri: window.location.origin,
    },
    cache: {
      cacheLocation: 'sessionStorage',
    },
  }
  return { instance: new PublicClientApplication(config), scope }
}

export async function ensureSignedIn(
  msal: PublicClientApplication,
  scope: string,
): Promise<AccountInfo> {
  await msal.initialize()
  const redirectResult = await msal.handleRedirectPromise()
  if (redirectResult?.account) {
    msal.setActiveAccount(redirectResult.account)
    const returnTo = window.sessionStorage.getItem(RETURN_TO_KEY)
    window.sessionStorage.removeItem(RETURN_TO_KEY)
    if (returnTo) {
      window.history.replaceState({}, '', returnTo)
      window.dispatchEvent(new PopStateEvent('popstate'))
    }
    return redirectResult.account
  }

  const existing = msal.getActiveAccount() ?? msal.getAllAccounts()[0]
  if (existing) {
    msal.setActiveAccount(existing)
    return existing
  }

  window.sessionStorage.setItem(
    RETURN_TO_KEY,
    window.location.pathname + window.location.search + window.location.hash,
  )
  await msal.loginRedirect({ scopes: [scope] })
  return new Promise(() => {
    // loginRedirect navigates the browser away; this promise never resolves.
  })
}

export async function getAccessToken(
  msal: PublicClientApplication,
  scope: string,
): Promise<string> {
  const account = msal.getActiveAccount() ?? msal.getAllAccounts()[0]
  if (!account) {
    throw new AuthConfigError('No signed-in account is available.')
  }
  try {
    const result = await msal.acquireTokenSilent({ scopes: [scope], account })
    return result.accessToken
  } catch (error: unknown) {
    if (error instanceof InteractionRequiredAuthError) {
      await msal.acquireTokenRedirect({ scopes: [scope], account })
      return new Promise(() => {
        // acquireTokenRedirect navigates the browser away; this promise never resolves.
      })
    }
    throw error
  }
}
