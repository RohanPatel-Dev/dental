# Clients

Two SPAs, deliberately separate applications rather than one app with a role switch.

| App | Port | Who uses it |
|---|---|---|
| `admin` | 5173 | The operator: the practices on the platform, their plans and their status. |
| `dashboard` | 5174 | A practice: patients, the diary and the day book. |

Aspire starts both (`AddViteApp`) and passes `VITE_DEV_PROXY_TARGET`, so no port is hard coded.

## The parts worth knowing about

* **`src/lib/config.ts`** — configuration is fetched from `/config.json` at boot, never baked into
  the bundle. One built artifact is promoted through every environment.
* **`src/lib/apiFetch.ts`** — the only place either app talks to the API. It owns the base URL, the
  tenant header, the bearer token, the single-flight token refresh, and turning ProblemDetails into
  a typed `ApiError`.
* **`src/lib/tokenStore.ts`** — storage keys are namespaced per app. Both SPAs are served from the
  API's origin in production, so unnamespaced keys would have them overwriting each other's session.
* **`src/lib/lazyNamed.ts`** — code splitting for components exported by name, so a page does not
  grow a default export purely to be routed to.
* **`src/lib/realtime.ts`** — SignalR is imported dynamically. It is ~55 KB, and most sessions never
  open a live screen.
* **`src/styles/globals.css`** — Tailwind v4 is CSS-first: the tokens in `@theme` are the config.
  There is no `tailwind.config.js`.

## Commands

```bash
npm install
npm run dev        # Vite, proxying /api to the host in VITE_DEV_PROXY_TARGET
npm run build      # tsc --noEmit, then vite build
npm run lint
npm run test:e2e   # Playwright
```

The Playwright specs mock the API at the network boundary, so they need the SPA and nothing else -
no API, no database, no seeded tenant. The contract they encode is checked from the other side by
the .NET integration tests. Set `PLAYWRIGHT_CHROMIUM_EXECUTABLE` to reuse a browser the machine
already has instead of downloading Playwright's pinned build.
