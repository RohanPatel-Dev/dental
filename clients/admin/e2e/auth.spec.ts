import { expect, test } from '@playwright/test';
import { adminUser } from './fixtures';

test.describe('sign-in', () => {
  test('sends the user to the sign-in screen without a token', async ({ page }) => {
    await page.goto('/tenants');

    await expect(page.getByRole('heading', { name: 'Operator console' })).toBeVisible();
  });

  test('signs in and lands on the practices list', async ({ page }) => {
    await page.route('**/api/v1/tokens', (route) =>
      route.fulfill({
        json: {
          accessToken: 'test-access-token',
          refreshToken: 'test-refresh-token',
          expiresAt: '2030-01-01T00:00:00Z',
        },
      }),
    );

    await page.route('**/api/v1/account/me', (route) => route.fulfill({ json: adminUser }));
    await page.route('**/api/v1/tenants**', (route) =>
      route.fulfill({
        json: {
          items: [],
          pageNumber: 1,
          pageSize: 20,
          totalCount: 0,
          totalPages: 0,
          hasPreviousPage: false,
          hasNextPage: false,
        },
      }),
    );

    await page.goto('/sign-in');
    await page.getByLabel('Practice').fill('root');
    await page.getByLabel('Email').fill('operator@dental.local');
    await page.getByLabel('Password').fill('Integration!Tests1');
    await page.getByRole('button', { name: 'Sign in' }).click();

    await expect(page.getByRole('heading', { name: 'Practices' })).toBeVisible();
  });

  test('reports a rejected sign-in without leaving the screen', async ({ page }) => {
    await page.route('**/api/v1/tokens', (route) =>
      route.fulfill({
        status: 401,
        contentType: 'application/problem+json',
        json: { title: 'Those credentials were not recognised.', status: 401 },
      }),
    );

    await page.goto('/sign-in');
    await page.getByLabel('Practice').fill('root');
    await page.getByLabel('Email').fill('operator@dental.local');
    await page.getByLabel('Password').fill('wrong');
    await page.getByRole('button', { name: 'Sign in' }).click();

    await expect(page.getByRole('alert')).toContainText('not recognised');
    await expect(page.getByRole('heading', { name: 'Operator console' })).toBeVisible();
  });
});
