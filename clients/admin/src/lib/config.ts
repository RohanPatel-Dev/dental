/**
 * Runtime configuration, fetched from /config.json at boot.
 *
 * NOT baked in at build time: one built artifact is promoted through every environment, and an API
 * base URL compiled into the bundle would mean rebuilding to deploy. The file is served next to the
 * bundle, so an operator edits it and reloads.
 */
export interface AppConfig {
  readonly apiBaseUrl: string;
  readonly realtimeUrl: string;
  /** Tenant this deployment is pinned to, when it serves one practice. Null means "ask the user". */
  readonly tenant: string | null;
  readonly environment: string;
  readonly features: Readonly<Record<string, boolean>>;
}

const fallback: AppConfig = {
  apiBaseUrl: '/api/v1',
  realtimeUrl: '/api/v1/realtime/hub',
  tenant: null,
  environment: 'unknown',
  features: {},
};

let current: AppConfig | null = null;

/** Loads /config.json once. Call before rendering; everything else reads it synchronously. */
export async function loadConfig(): Promise<AppConfig> {
  if (current) {
    return current;
  }

  try {
    const response = await fetch('/config.json', { cache: 'no-store' });
    if (!response.ok) {
      throw new Error(`config.json responded ${response.status}`);
    }

    current = { ...fallback, ...((await response.json()) as Partial<AppConfig>) };
  } catch (error) {
    // A missing config file must not blank the screen: the defaults describe the same-origin case,
    // which is how the app is deployed behind the API.
    console.warn('Falling back to the built-in configuration.', error);
    current = fallback;
  }

  return current;
}

/** The loaded configuration. Throws when called before loadConfig, which is a wiring bug. */
export function config(): AppConfig {
  if (!current) {
    throw new Error('loadConfig() must complete before the application reads its configuration.');
  }

  return current;
}
