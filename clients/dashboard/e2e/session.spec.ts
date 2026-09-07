import { expect, test } from '@playwright/test';
import { pageOf, practiceUser, signInAs } from './fixtures';

test.describe('session', () => {
  test('refreshes an expired access token and replays the request', async ({ page }) => {
    // The refresh path is invisible in normal use and easy to break, so it is pinned here: one 401
    // must produce exactly one refresh and one replay, not a sign-out.
    await signInAs(page);

    let served401 = false;
    let refreshes = 0;

    await page.route('**/api/v1/account/me', (route) => route.fulfill({ json: practiceUser }));

    await page.route('**/api/v1/tokens/refresh', (route) => {
      refreshes += 1;
      return route.fulfill({
        json: {
          accessToken: 'fresh-access-token',
          refreshToken: 'fresh-refresh-token',
          expiresAt: '2030-01-01T00:00:00Z',
        },
      });
    });

    await page.route('**/api/v1/patients?**', (route) => {
      if (!served401) {
        served401 = true;
        return route.fulfill({
          status: 401,
          contentType: 'application/problem+json',
          json: { title: 'Authentication is required.', status: 401 },
        });
      }

      return route.fulfill({ json: pageOf([]) });
    });

    await page.goto('/patients');

    await expect(page.getByText('No patient matches that search.')).toBeVisible();
    expect(refreshes).toBe(1);
  });

  test('signs the user out when the refresh token is spent', async ({ page }) => {
    await signInAs(page);

    await page.route('**/api/v1/account/me', (route) => route.fulfill({ json: practiceUser }));
    await page.route('**/api/v1/tokens/refresh', (route) =>
      route.fulfill({ status: 401, json: { title: 'That refresh token is spent.', status: 401 } }),
    );
    await page.route('**/api/v1/patients?**', (route) =>
      route.fulfill({ status: 401, json: { title: 'Authentication is required.', status: 401 } }),
    );

    await page.goto('/patients');

    await expect(page.getByRole('heading', { name: 'Sign in to your practice' })).toBeVisible();
  });
});
