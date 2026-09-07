/**
 * Token storage, namespaced per application.
 *
 * Both SPAs are served from the same origin as the API in production, so they share one
 * localStorage. An unnamespaced key would have the operator console and the practice app
 * overwriting each other's session on every sign-in.
 */
const namespace = 'dental.dashboard';

const keys = {
  access: `${namespace}.accessToken`,
  refresh: `${namespace}.refreshToken`,
  tenant: `${namespace}.tenant`,
} as const;

export interface StoredTokens {
  readonly accessToken: string;
  readonly refreshToken: string;
  readonly tenant: string;
}

function read(key: string): string | null {
  try {
    return window.localStorage.getItem(key);
  } catch {
    // Private windows and "block site data" throw on access rather than returning null.
    return null;
  }
}

function write(key: string, value: string): void {
  try {
    window.localStorage.setItem(key, value);
  } catch {
    // Nothing to do: the session simply does not survive a reload.
  }
}

export const tokenStore = {
  read(): StoredTokens | null {
    const accessToken = read(keys.access);
    const refreshToken = read(keys.refresh);
    const tenant = read(keys.tenant);

    return accessToken && refreshToken && tenant ? { accessToken, refreshToken, tenant } : null;
  },

  save(tokens: StoredTokens): void {
    write(keys.access, tokens.accessToken);
    write(keys.refresh, tokens.refreshToken);
    write(keys.tenant, tokens.tenant);
  },

  clear(): void {
    for (const key of Object.values(keys)) {
      try {
        window.localStorage.removeItem(key);
      } catch {
        // Ignored, as above.
      }
    }
  },
};
