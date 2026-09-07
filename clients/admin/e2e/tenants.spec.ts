import { expect, test } from '@playwright/test';
import { mockApi, signInAs } from './fixtures';

test.describe('practices', () => {
  test.beforeEach(async ({ page }) => {
    await signInAs(page);
    await mockApi(page);
  });

  test('lists the practices on the platform', async ({ page }) => {
    await page.goto('/tenants');

    await expect(page.getByRole('heading', { name: 'Practices' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Smile Dental' })).toBeVisible();
  });

  test('provisions a practice and shows it in the list', async ({ page }) => {
    let created = false;

    await page.route('**/api/v1/tenants', async (route) => {
      if (route.request().method() !== 'POST') {
        return route.fulfill({ json: created ? withNewPractice() : tenantPageEmpty() });
      }

      // The client must send an idempotency key: provisioning fans out through the outbox, so a
      // repeated POST would seed a second practice.
      expect(await route.request().headerValue('Idempotency-Key')).not.toBeNull();
      created = true;

      return route.fulfill({
        json: {
          id: '018f0000-0000-7000-8000-000000000011',
          identifier: 'new-practice',
          name: 'New Practice',
          plan: 'solo',
          isActive: true,
          timeZone: 'Europe/London',
          validUntil: null,
          createdAt: '2026-09-07T09:00:00Z',
        },
      });
    });

    await page.route('**/api/v1/tenants?**', (route) =>
      route.fulfill({ json: created ? withNewPractice() : tenantPageEmpty() }),
    );

    await page.goto('/tenants');
    await page.getByRole('button', { name: 'Add a practice' }).click();

    await page.getByLabel('Identifier').fill('new-practice');
    await page.getByLabel('Name').fill('New Practice');
    await page.getByLabel('Administrator email').fill('admin@new-practice.test');
    await page.getByRole('button', { name: 'Provision practice' }).click();

    await expect(page.getByRole('link', { name: 'New Practice' })).toBeVisible();
  });

  test('shows the API problem when provisioning is refused', async ({ page }) => {
    await page.route('**/api/v1/tenants', (route) =>
      route.request().method() === 'POST'
        ? route.fulfill({
            status: 409,
            contentType: 'application/problem+json',
            json: {
              title: 'A practice with that identifier already exists.',
              status: 409,
              correlationId: 'corr-1234',
            },
          })
        : route.fulfill({ json: tenantPageEmpty() }),
    );

    await page.goto('/tenants');
    await page.getByRole('button', { name: 'Add a practice' }).click();
    await page.getByLabel('Identifier').fill('smile-dental');
    await page.getByLabel('Name').fill('Smile Dental');
    await page.getByLabel('Administrator email').fill('admin@smile.test');
    await page.getByRole('button', { name: 'Provision practice' }).click();

    const alert = page.getByRole('alert');
    await expect(alert).toContainText('already exists');
    await expect(alert).toContainText('corr-1234');
  });
});

function tenantPageEmpty() {
  return {
    items: [],
    pageNumber: 1,
    pageSize: 20,
    totalCount: 0,
    totalPages: 0,
    hasPreviousPage: false,
    hasNextPage: false,
  };
}

function withNewPractice() {
  return {
    ...tenantPageEmpty(),
    items: [
      {
        id: '018f0000-0000-7000-8000-000000000011',
        identifier: 'new-practice',
        name: 'New Practice',
        plan: 'solo',
        isActive: true,
        timeZone: 'Europe/London',
        validUntil: null,
        createdAt: '2026-09-07T09:00:00Z',
      },
    ],
    totalCount: 1,
    totalPages: 1,
  };
}
