import type { Page } from '@playwright/test';

/**
 * Mocks the API at the network boundary.
 *
 * The suite runs against the SPA alone: no API, no database, no seeded practice. That keeps it fast
 * enough for every commit, and means a failure is a bug in the SPA rather than in the environment.
 * The contract these routes encode is checked on the other side by the .NET integration tests.
 */
export const practiceUser = {
  user: {
    id: '018f0000-0000-7000-8000-000000000001',
    email: 'admin@smile-dental.test',
    firstName: 'Priya',
    lastName: 'Shah',
    fullName: 'Priya Shah',
    roles: ['Admin'],
  },
  permissions: [
    'Permissions.Patients.Search',
    'Permissions.Patients.View',
    'Permissions.Patients.Create',
    'Permissions.Patients.Update',
    'Permissions.Appointments.Search',
    'Permissions.Invoices.Search',
  ],
  tenantId: 'smile-dental',
};

export const patient = {
  id: '018f0000-0000-7000-8000-000000000020',
  chartNumber: 'P-000001',
  firstName: 'Ada',
  lastName: 'Lovelace',
  fullName: 'Ada Lovelace',
  dateOfBirth: '1985-04-12',
  sex: 'Female',
  email: 'ada@example.test',
  phoneNumber: '+15551234567',
  allergies: ['Penicillin'],
  hasMarketingConsent: false,
  hasReminderConsent: true,
  isActive: true,
};

export function pageOf<T>(items: readonly T[]) {
  return {
    items,
    pageNumber: 1,
    pageSize: 20,
    totalCount: items.length,
    totalPages: items.length === 0 ? 0 : 1,
    hasPreviousPage: false,
    hasNextPage: false,
  };
}

/** Signs in by seeding the token store, so specs do not repeat the sign-in flow. */
export async function signInAs(page: Page): Promise<void> {
  await page.addInitScript(() => {
    window.localStorage.setItem('dental.dashboard.accessToken', 'test-access-token');
    window.localStorage.setItem('dental.dashboard.refreshToken', 'test-refresh-token');
    window.localStorage.setItem('dental.dashboard.tenant', 'smile-dental');
  });
}

export async function mockApi(page: Page): Promise<void> {
  await page.route('**/api/v1/account/me', (route) => route.fulfill({ json: practiceUser }));
  await page.route('**/api/v1/patients?**', (route) => route.fulfill({ json: pageOf([patient]) }));
  await page.route(`**/api/v1/patients/${patient.id}`, (route) => route.fulfill({ json: patient }));
}
