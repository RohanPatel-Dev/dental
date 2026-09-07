import type { Page } from '@playwright/test';

/**
 * Mocks the API at the network boundary.
 *
 * The suite runs against the SPA alone: no API, no database, no seeded tenant. That is what lets it
 * run on every commit in seconds, and it means a failure is a bug in the SPA rather than in the
 * environment. The contract these routes encode is checked on the other side by the .NET
 * integration tests.
 */
export const adminUser = {
  user: {
    id: '018f0000-0000-7000-8000-000000000001',
    email: 'operator@dental.local',
    firstName: 'Ops',
    lastName: 'Admin',
    fullName: 'Ops Admin',
    roles: ['Admin'],
  },
  permissions: [
    'Permissions.Tenants.Search',
    'Permissions.Tenants.View',
    'Permissions.Tenants.Create',
    'Permissions.Tenants.Update',
    'Permissions.Users.Search',
  ],
  tenantId: 'root',
};

export const tenantPage = {
  items: [
    {
      id: '018f0000-0000-7000-8000-000000000010',
      identifier: 'smile-dental',
      name: 'Smile Dental',
      plan: 'practice',
      isActive: true,
      timeZone: 'Europe/London',
      validUntil: null,
      createdAt: '2026-01-04T09:00:00Z',
    },
  ],
  pageNumber: 1,
  pageSize: 20,
  totalCount: 1,
  totalPages: 1,
  hasPreviousPage: false,
  hasNextPage: false,
};

/** Signs in by seeding the token store, so specs do not repeat the sign-in flow. */
export async function signInAs(page: Page): Promise<void> {
  await page.addInitScript(() => {
    window.localStorage.setItem('dental.admin.accessToken', 'test-access-token');
    window.localStorage.setItem('dental.admin.refreshToken', 'test-refresh-token');
    window.localStorage.setItem('dental.admin.tenant', 'root');
  });
}

export async function mockApi(page: Page): Promise<void> {
  await page.route('**/api/v1/account/me', (route) =>
    route.fulfill({ json: adminUser }),
  );

  await page.route('**/api/v1/tenants?**', (route) => route.fulfill({ json: tenantPage }));
  await page.route('**/api/v1/tenants', (route) => route.fulfill({ json: tenantPage }));
}
