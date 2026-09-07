import { config } from './config';
import { ApiError } from './problem';
import { tokenStore } from './tokenStore';

/**
 * The ONE way this app talks to the API.
 *
 * It owns the four things every call needs and no component should repeat: the base URL, the tenant
 * header, the bearer token, and turning a ProblemDetails body into a typed error. It also refreshes
 * an expired access token once and replays the request - concurrent calls share a single refresh,
 * so a page that fires six queries at once does not burn six refresh tokens.
 */
const appHeader = 'admin';

let refreshInFlight: Promise<string | null> | null = null;

export interface ApiFetchOptions extends Omit<RequestInit, 'body'> {
  readonly body?: unknown;
  /** Makes a POST replay safe. The same key returns the first response instead of repeating work. */
  readonly idempotencyKey?: string;
  /** Set for sign-in, which has no token yet and must not trigger a refresh. */
  readonly anonymous?: boolean;
}

export async function apiFetch<T>(path: string, options: ApiFetchOptions = {}): Promise<T> {
  const response = await send(path, options);

  if (response.status === 401 && !options.anonymous) {
    const refreshed = await refreshOnce();
    if (refreshed) {
      return unwrap<T>(await send(path, options));
    }

    tokenStore.clear();
    window.dispatchEvent(new CustomEvent('dental:signed-out'));
  }

  return unwrap<T>(response);
}

async function send(path: string, options: ApiFetchOptions): Promise<Response> {
  const { body, idempotencyKey, anonymous, headers, ...rest } = options;
  const stored = tokenStore.read();

  const merged = new Headers(headers);
  merged.set('Accept', 'application/json');
  merged.set('X-App', appHeader);

  if (body !== undefined) {
    merged.set('Content-Type', 'application/json');
  }

  if (idempotencyKey) {
    merged.set('Idempotency-Key', idempotencyKey);
  }

  const tenant = stored?.tenant ?? config().tenant;
  if (tenant) {
    merged.set('tenant', tenant);
  }

  if (!anonymous && stored) {
    merged.set('Authorization', `Bearer ${stored.accessToken}`);
  }

  return fetch(`${config().apiBaseUrl}${path}`, {
    ...rest,
    headers: merged,
    ...(body === undefined ? {} : { body: JSON.stringify(body) }),
  });
}

async function unwrap<T>(response: Response): Promise<T> {
  if (response.status === 204 || response.headers.get('Content-Length') === '0') {
    return undefined as T;
  }

  const text = await response.text();
  const parsed: unknown = text.length > 0 ? JSON.parse(text) : undefined;

  if (!response.ok) {
    // Every ProblemDetails member is optional, so an empty body is a valid (if unhelpful) problem
    // and needs no assertion.
    throw new ApiError(response.status, parsed ?? {});
  }

  return parsed as T;
}

/** Refreshes the access token, collapsing concurrent callers onto one request. */
function refreshOnce(): Promise<string | null> {
  refreshInFlight ??= (async () => {
    const stored = tokenStore.read();
    if (!stored) {
      return null;
    }

    try {
      const response = await fetch(`${config().apiBaseUrl}/tokens/refresh`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Accept: 'application/json',
          tenant: stored.tenant,
          'X-App': appHeader,
        },
        body: JSON.stringify({ refreshToken: stored.refreshToken }),
      });

      if (!response.ok) {
        return null;
      }

      const tokens = (await response.json()) as { accessToken: string; refreshToken: string };
      tokenStore.save({ ...tokens, tenant: stored.tenant });
      return tokens.accessToken;
    } catch {
      return null;
    } finally {
      refreshInFlight = null;
    }
  })();

  return refreshInFlight;
}
