# Frontend rules

Applies to everything under `clients/`.

* **Never call `fetch` directly.** `apiFetch` owns the base URL, the tenant header, the bearer
  token, the single-flight refresh and ProblemDetails handling. A bare fetch skips all five.
* **Never read `import.meta.env` for anything environment-specific.** Configuration comes from
  `/config.json` at runtime, so one built artifact is promoted through every environment.
* **Never touch `localStorage` outside `tokenStore`.** Keys are namespaced per app because both SPAs
  are served from the API's origin in production.
* Route to pages through `lazyNamed`, so every page is code split and keeps its named export.
* Import SignalR **dynamically**, through `lib/realtime.ts`. A static import puts 55 KB in the entry
  chunk for every session, including the ones that never open a live screen.
* Server state is TanStack Query. A 4xx is an answer, not a blip: `queryClient` already declines to
  retry it, so do not override that per query.
* A POST that creates something sends an `Idempotency-Key`.
* Tailwind v4 is CSS-first. Tokens live in `@theme` in `globals.css`; there is no config file. A
  reusable class is a `@utility`, not a `@layer components` rule, or `@apply` cannot compose it.
* Show `ApiError.correlationId` on failure. It is what support asks for.
* Playwright specs mock the API at the network boundary — no API, no database, no seeded tenant.
