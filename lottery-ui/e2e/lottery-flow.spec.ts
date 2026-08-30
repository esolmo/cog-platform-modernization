import { test, expect, Page } from '@playwright/test'

// ── Helpers ────────────────────────────────────────────────────────────────────
//
// lottery-ui has no login page of its own — it reads a bare "accessToken" out of
// localStorage (see src/api/lottery.ts) and assumes something else (another app
// sharing the same origin/session) already put a valid JWT there. There's no such
// flow in local dev, so this spec authenticates directly against auth-service and
// seeds the token into this page's origin before navigating anywhere.

async function loginAs(page: Page, username = 'seedcust1', password = 'P@ssw0rd!') {
  const authRes = await page.request.post('http://localhost:5010/api/auth/login', {
    data: { loginName: username, password },
  })
  expect(authRes.ok()).toBeTruthy()
  const { accessToken } = await authRes.json()
  await page.addInitScript((token) => {
    window.localStorage.setItem('accessToken', token as string)
  }, accessToken)
}

// ── Games page ─────────────────────────────────────────────────────────────────

test.describe('Games page', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page)
  })

  test('shows the COG Lottery heading and both seeded games', async ({ page }) => {
    await page.goto('/games')
    await expect(page.getByRole('heading', { name: 'COG Lottery' })).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Pick 3', level: 3 })).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Pick 4', level: 3 })).toBeVisible()
  })

  test('My Tickets link navigates to history', async ({ page }) => {
    await page.goto('/games')
    await page.getByRole('link', { name: 'My Tickets' }).click()
    await expect(page).toHaveURL('/history')
  })

  test('selecting a game navigates to its pick page', async ({ page }) => {
    await page.goto('/games')
    await page.getByRole('link', { name: /Pick 3/ }).click()
    await expect(page).toHaveURL(/\/games\/\d+\/pick/)
    await expect(page.getByRole('heading', { name: 'Pick 3' })).toBeVisible()
  })
})

// ── Pick page — purchase flow ───────────────────────────────────────────────────

test.describe('Pick page', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page)
  })

  test('lists upcoming drawings in the Drawing dropdown', async ({ page }) => {
    await page.goto('/games/1/pick')
    const options = page.getByLabel('Drawing').locator('option')
    await expect(options).not.toHaveCount(0)
  })

  test('Pick Type defaults to Straight', async ({ page }) => {
    await page.goto('/games/1/pick')
    await expect(page.getByRole('radio', { name: 'Straight' })).toBeChecked()
    await expect(page.getByRole('radio', { name: 'Boxed (all permutations)' })).not.toBeChecked()
  })

  test('+ Add Line adds another row of number inputs, Remove removes it', async ({ page }) => {
    await page.goto('/games/1/pick')
    await expect(page.locator('input[name="picks.1.number1"]')).toHaveCount(0)

    await page.getByRole('button', { name: '+ Add Line' }).click()
    await expect(page.locator('input[name="picks.1.number1"]')).toHaveCount(1)

    await page.getByRole('button', { name: 'Remove' }).first().click()
    await expect(page.locator('input[name="picks.1.number1"]')).toHaveCount(0)
  })

  test('purchasing a Pick 3 straight ticket succeeds and redirects to history', async ({ page }) => {
    await page.goto('/games/1/pick')

    // The seeded drawings include some in the past (today's already-occurred draws) —
    // picking the last <option> keeps this robust against exactly which ones exist.
    const drawingSelect = page.getByLabel('Drawing')
    const lastValue = await drawingSelect.locator('option').last().getAttribute('value')
    await drawingSelect.selectOption(lastValue!)

    await page.locator('input[name="picks.0.number1"]').fill('1')
    await page.locator('input[name="picks.0.number2"]').fill('2')
    await page.locator('input[name="picks.0.number3"]').fill('3')
    await page.locator('input[name="picks.0.amount"]').fill('1')

    await page.getByRole('button', { name: 'Purchase Ticket' }).click()

    await expect(page.getByText(/Ticket #\d+ purchased!/)).toBeVisible({ timeout: 5000 })
    await expect(page).toHaveURL('/history', { timeout: 5000 })
  })

  test('purchasing a Pick 4 boxed ticket succeeds', async ({ page }) => {
    await page.goto('/games/2/pick')
    await expect(page.getByRole('heading', { name: 'Pick 4' })).toBeVisible()
    // Pick 4 shows a 4th number input that Pick 3 doesn't.
    await expect(page.locator('input[name="picks.0.number4"]')).toBeVisible()

    const drawingSelect = page.getByLabel('Drawing')
    const lastValue = await drawingSelect.locator('option').last().getAttribute('value')
    await drawingSelect.selectOption(lastValue!)

    await page.getByRole('radio', { name: 'Boxed (all permutations)' }).check()
    await page.locator('input[name="picks.0.number1"]').fill('1')
    await page.locator('input[name="picks.0.number2"]').fill('2')
    await page.locator('input[name="picks.0.number3"]').fill('3')
    await page.locator('input[name="picks.0.number4"]').fill('4')
    await page.locator('input[name="picks.0.amount"]').fill('24')

    await page.getByRole('button', { name: 'Purchase Ticket' }).click()

    await expect(page.getByText(/Ticket #\d+ purchased!/)).toBeVisible({ timeout: 5000 })
  })
})

// ── History & ticket detail ─────────────────────────────────────────────────────

test.describe('History and ticket detail', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page)
  })

  test('a purchased ticket appears in history and links to its detail page', async ({ page }) => {
    // Purchase a ticket first so history is guaranteed non-empty.
    await page.goto('/games/1/pick')
    const drawingSelect = page.getByLabel('Drawing')
    const lastValue = await drawingSelect.locator('option').last().getAttribute('value')
    await drawingSelect.selectOption(lastValue!)
    await page.locator('input[name="picks.0.number1"]').fill('7')
    await page.locator('input[name="picks.0.number2"]').fill('7')
    await page.locator('input[name="picks.0.number3"]').fill('7')
    await page.locator('input[name="picks.0.amount"]').fill('2')
    await page.getByRole('button', { name: 'Purchase Ticket' }).click()
    await expect(page).toHaveURL('/history', { timeout: 5000 })

    await expect(page.getByRole('heading', { name: 'My Tickets' })).toBeVisible()
    const firstTicket = page.locator('a[href^="/tickets/"]').first()
    await expect(firstTicket).toBeVisible()

    await firstTicket.click()
    await expect(page).toHaveURL(/\/tickets\/\d+/)
    await expect(page.getByRole('heading', { name: /Ticket #\d+/ })).toBeVisible()
    await expect(page.getByText('Drawing')).toBeVisible()
    await expect(page.getByText('Picks (', { exact: false })).toBeVisible()
  })

  test('ticket detail — Back to My Tickets link returns to history', async ({ page }) => {
    await page.goto('/games/1/pick')
    const drawingSelect = page.getByLabel('Drawing')
    const lastValue = await drawingSelect.locator('option').last().getAttribute('value')
    await drawingSelect.selectOption(lastValue!)
    await page.locator('input[name="picks.0.number1"]').fill('4')
    await page.locator('input[name="picks.0.number2"]').fill('5')
    await page.locator('input[name="picks.0.number3"]').fill('6')
    await page.locator('input[name="picks.0.amount"]').fill('1')
    await page.getByRole('button', { name: 'Purchase Ticket' }).click()
    await expect(page).toHaveURL('/history', { timeout: 5000 })

    await page.locator('a[href^="/tickets/"]').first().click()
    await expect(page).toHaveURL(/\/tickets\/\d+/)

    await page.getByRole('link', { name: /My Tickets/ }).click()
    await expect(page).toHaveURL('/history')
  })
})
