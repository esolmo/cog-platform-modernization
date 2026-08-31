import { test, expect, Page } from '@playwright/test'

// ── Helpers ────────────────────────────────────────────────────────────────────
//
// reports-ui has no login page of its own — it reads a bare "accessToken" out of
// localStorage (see src/api/reports.ts) and assumes something else (another app
// sharing the same origin/session) already put a valid JWT there. There's no such
// flow in local dev, so this spec authenticates directly against auth-service and
// seeds the token into this page's origin before navigating anywhere.

async function loginAs(page: Page, username = 'admin', password = 'Admin123!') {
  const authRes = await page.request.post('http://localhost:5010/api/auth/login', {
    data: { loginName: username, password },
  })
  expect(authRes.ok()).toBeTruthy()
  const { accessToken } = await authRes.json()
  await page.addInitScript((token) => {
    window.localStorage.setItem('accessToken', token as string)
  }, accessToken)
}

// ── Navigation & layout ──────────────────────────────────────────────────────────

test.describe('Layout & navigation', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page)
  })

  test('redirects "/" to "/wagers" and shows the COG Reports heading', async ({ page }) => {
    await page.goto('/')
    await expect(page).toHaveURL('/wagers')
    await expect(page.getByRole('heading', { name: 'COG Reports' })).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Wager Activity' })).toBeVisible()
  })

  test('sidebar links navigate between the three reports', async ({ page }) => {
    await page.goto('/wagers')

    await page.getByRole('link', { name: 'Changed Transactions' }).click()
    await expect(page).toHaveURL('/transactions')
    await expect(page.getByRole('heading', { name: 'Changed Transactions' })).toBeVisible()

    await page.getByRole('link', { name: 'Agents & Customers' }).click()
    await expect(page).toHaveURL('/agents')
    await expect(page.getByRole('heading', { name: 'Agents & Customers' })).toBeVisible()

    await page.getByRole('link', { name: 'Wager Activity' }).click()
    await expect(page).toHaveURL('/wagers')
  })
})

// ── Wager Activity report ─────────────────────────────────────────────────────────

test.describe('Wager Activity report', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page)
    await page.goto('/wagers')
  })

  test('Run Report is disabled until a Login ID is entered', async ({ page }) => {
    await expect(page.getByRole('button', { name: 'Run Report' })).toBeDisabled()
    await page.getByLabel('Login ID').fill('agent1')
    await expect(page.getByRole('button', { name: 'Run Report' })).toBeEnabled()
  })

  test('running the report for the seeded agent1 login shows results and a total', async ({ page }) => {
    await page.getByLabel('Login ID').fill('agent1')
    await page.getByLabel('From').fill('2026-08-01')
    await page.getByLabel('To').fill('2026-09-01')
    await page.getByRole('button', { name: 'Run Report' }).click()

    await expect(page.getByText(/\d+ records/)).toBeVisible({ timeout: 5000 })
    await expect(page.getByText('DOC-E2E-1')).toBeVisible()
    await expect(page.getByText('DOC-E2E-2')).toBeVisible()
    await expect(page.getByText(/Total: \$150\.00/)).toBeVisible()
  })

  test('a login with no activity in range shows the empty state', async ({ page }) => {
    await page.getByLabel('Login ID').fill('nobody-with-no-wagers')
    await page.getByRole('button', { name: 'Run Report' }).click()

    await expect(page.getByText('No results found for this period.')).toBeVisible({ timeout: 5000 })
  })

  test('sorting by clicking the Amount column header does not error', async ({ page }) => {
    await page.getByLabel('Login ID').fill('agent1')
    await page.getByRole('button', { name: 'Run Report' }).click()
    await expect(page.getByText(/\d+ records/)).toBeVisible({ timeout: 5000 })

    await page.getByRole('columnheader', { name: /Amount/ }).click()
    // Table still renders both seeded rows after sorting.
    await expect(page.getByText('DOC-E2E-1')).toBeVisible()
    await expect(page.getByText('DOC-E2E-2')).toBeVisible()
  })
})

// ── Changed Transactions report ───────────────────────────────────────────────────

test.describe('Changed Transactions report', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page)
    await page.goto('/transactions')
  })

  test('Run Report is disabled until an Agent ID is entered', async ({ page }) => {
    await expect(page.getByRole('button', { name: 'Run Report' })).toBeDisabled()
    await page.getByLabel('Agent ID').fill('1')
    await expect(page.getByRole('button', { name: 'Run Report' })).toBeEnabled()
  })

  test('running the report for the seeded agent shows the seeded customer transaction', async ({ page }) => {
    await page.getByLabel('Agent ID').fill('1')
    await page.getByLabel('From', { exact: true }).fill('2026-08-01')
    await page.getByLabel('To', { exact: true }).fill('2026-09-01')
    await page.getByRole('button', { name: 'Run Report' }).click()

    await expect(page.getByText(/\d+ records/)).toBeVisible({ timeout: 5000 })
    await expect(page.getByText('e2eplayer1')).toBeVisible()
    await expect(page.getByText('REF-E2E-1')).toBeVisible()
  })

  test('Include agent transactions checkbox toggles', async ({ page }) => {
    const checkbox = page.getByLabel('Include agent transactions')
    await expect(checkbox).not.toBeChecked()
    await checkbox.check()
    await expect(checkbox).toBeChecked()
  })
})

// ── Agents & Customers report ─────────────────────────────────────────────────────

test.describe('Agents & Customers report', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page)
    await page.goto('/agents')
  })

  test('defaults to Agents mode and finds the seeded agent1', async ({ page }) => {
    await expect(page.getByRole('radio', { name: 'Agents' })).toBeChecked()

    await page.getByLabel('Agent ID').fill('1')
    await page.getByLabel('Search').fill('agent')
    await page.getByRole('button', { name: 'Search' }).click()

    await expect(page.getByText(/\d+ results/)).toBeVisible({ timeout: 5000 })
    await expect(page.getByText('agent1', { exact: true })).toBeVisible()
    await expect(page.getByText('E2E Test Agent')).toBeVisible()
  })

  test('switching to Customers mode finds the seeded e2eplayer1', async ({ page }) => {
    await page.getByLabel('Agent ID').fill('1')
    await page.getByLabel('Search').fill('e2e')
    await page.getByRole('radio', { name: 'Customers' }).check()
    await page.getByRole('button', { name: 'Search' }).click()

    await expect(page.getByText(/\d+ results/)).toBeVisible({ timeout: 5000 })
    await expect(page.getByText('e2eplayer1')).toBeVisible()
    await expect(page.getByText('E2E Player One')).toBeVisible()
    await expect(page.getByText(/\$150\.00/)).toBeVisible()
  })

  test('a search with no matches shows the empty state', async ({ page }) => {
    await page.getByLabel('Agent ID').fill('1')
    await page.getByLabel('Search').fill('zzz-no-such-agent')
    await page.getByRole('button', { name: 'Search' }).click()

    await expect(page.getByText('No results found.')).toBeVisible({ timeout: 5000 })
  })
})
