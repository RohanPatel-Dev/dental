import { apiFetch } from '@/lib/apiFetch';
import { tokenStore } from '@/lib/tokenStore';

export interface TokenPair {
  readonly accessToken: string;
  readonly refreshToken: string;
  readonly expiresAt: string;
}

export interface UserSummary {
  readonly id: string;
  readonly email: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly fullName: string;
  readonly roles: readonly string[];
}

export interface CurrentUser {
  readonly user: UserSummary;
  readonly permissions: readonly string[];
  readonly tenantId: string;
}

/** Exchanges credentials for a token pair and stores it under this app's namespace. */
export async function signIn(tenant: string, email: string, password: string): Promise<void> {
  const tokens = await apiFetch<TokenPair>('/tokens', {
    method: 'POST',
    body: { email, password },
    anonymous: true,
    headers: { tenant },
  });

  tokenStore.save({ accessToken: tokens.accessToken, refreshToken: tokens.refreshToken, tenant });
}

/** Who the stored token belongs to, and what it may do. */
export function fetchCurrentUser(): Promise<CurrentUser> {
  return apiFetch<CurrentUser>('/account/me');
}
