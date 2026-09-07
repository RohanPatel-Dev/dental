import { expect, test } from '@playwright/test';
import { mockApi, pageOf, patient, signInAs } from './fixtures';

test.describe('patients', () => {
  test.beforeEach(async ({ page }) => {
    await signInAs(page);
    await mockApi(page);
  });

  test('lists the practice patients', async ({ page }) => {
    await page.goto('/patients');

    await expect(page.getByRole('heading', { name: 'Patients' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Ada Lovelace' })).toBeVisible();
    await expect(page.getByText('P-000001')).toBeVisible();
  });

  test('opens a patient and shows their consent', async ({ page }) => {
    await page.goto('/patients');
    await page.getByRole('link', { name: 'Ada Lovelace' }).click();

    await expect(page.getByRole('heading', { name: 'Ada Lovelace' })).toBeVisible();
    await expect(page.getByLabel('Appointment reminders')).toBeChecked();
    await expect(page.getByLabel('Marketing contact')).not.toBeChecked();
  });

  test('registers a patient with an idempotency key', async ({ page }) => {
    let registered = false;

    await page.route('**/api/v1/patients', async (route) => {
      expect(route.request().method()).toBe('POST');
      expect(await route.request().headerValue('Idempotency-Key')).not.toBeNull();
      registered = true;
      return route.fulfill({ json: { ...patient, firstName: 'Grace', fullName: 'Grace Hopper' } });
    });

    await page.route('**/api/v1/patients?**', (route) =>
      route.fulfill({
        json: pageOf(registered ? [{ ...patient, fullName: 'Grace Hopper' }] : []),
      }),
    );

    await page.goto('/patients');
    await page.getByRole('button', { name: 'Register a patient' }).click();

    await page.getByLabel('First name').fill('Grace');
    await page.getByLabel('Last name').fill('Hopper');
    await page.getByLabel('Date of birth').fill('1906-12-09');
    await page.getByLabel('Email').fill('grace@example.test');
    await page.getByRole('button', { name: 'Register patient' }).click();

    await expect(page.getByRole('link', { name: 'Grace Hopper' })).toBeVisible();
  });

  test('shows the per-field errors from a validation problem', async ({ page }) => {
    await page.route('**/api/v1/patients', (route) =>
      route.fulfill({
        status: 400,
        contentType: 'application/problem+json',
        json: {
          title: 'One or more validation errors occurred.',
          status: 400,
          errors: { Contact: ['Either an email address or a phone number is required.'] },
          correlationId: 'corr-4242',
        },
      }),
    );

    await page.goto('/patients');
    await page.getByRole('button', { name: 'Register a patient' }).click();
    await page.getByLabel('First name').fill('Nobody');
    await page.getByLabel('Last name').fill('Contactable');
    await page.getByLabel('Date of birth').fill('1990-01-01');
    await page.getByRole('button', { name: 'Register patient' }).click();

    const alert = page.getByRole('alert');
    await expect(alert).toContainText('Contact');
    await expect(alert).toContainText('email address or a phone number');
    await expect(alert).toContainText('corr-4242');
  });
});
