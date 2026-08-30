import { test, expect, Page } from '@playwright/test'

// ── Helpers ────────────────────────────────────────────────────────────────────
//
// NOTE: this spec was rewritten to match the shipped betting-ui implementation.
// The previous version referenced data-testid hooks and a bet-type/side picker
// step on the wager-entry page that were never implemented — the real flow
// picks a specific line (type + side + price) directly from the game list, and
// customer/risk/wager-type are entered on a single subsequent form.

async function loginAs(page: Page, username = 'agent1', password = 'P@ssw0rd!') {
  await page.goto('/login')
  await page.getByLabel('Username').fill(username)
  await page.getByLabel('Password').fill(password)
  await page.getByRole('button', { name: 'Sign In' }).click()
  await expect(page).not.toHaveURL('/login', { timeout: 5000 })
}

// ── Login page ─────────────────────────────────────────────────────────────────

test.describe('Login page', () => {
  test('renders all form elements', async ({ page }) => {
    await page.goto('/login')
    await expect(page.getByRole('heading', { name: 'COG Betting' })).toBeVisible()
    await expect(page.getByLabel('Username')).toBeVisible()
    await expect(page.getByLabel('Password')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Sign In' })).toBeVisible()
  })

  test('validates empty submission', async ({ page }) => {
    await page.goto('/login')
    await page.getByRole('button', { name: 'Sign In' }).click()
    await expect(page.getByText('Username is required')).toBeVisible()
    await expect(page.getByText('Password is required')).toBeVisible()
  })

  test('shows error for bad credentials', async ({ page }) => {
    await page.goto('/login')
    await page.getByLabel('Username').fill('nobody')
    await page.getByLabel('Password').fill('wrongpass')
    await page.getByRole('button', { name: 'Sign In' }).click()
    await expect(page.getByText(/invalid credentials/i)).toBeVisible({ timeout: 5000 })
  })

  test('redirects unauthenticated user to login', async ({ page }) => {
    await page.goto('/sports')
    await expect(page).toHaveURL('/login')
  })
})

// ── Full betting flow: Login → Sport → Game/Line → Wager → Confirmation ───────

test.describe('Betting flow — happy path', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page)
  })

  test('lands on sport selection after login', async ({ page }) => {
    await expect(page).toHaveURL('/sports')
    await expect(page.getByRole('heading', { name: 'Select Sport' })).toBeVisible()
  })

  test('sport cards are listed', async ({ page }) => {
    await page.goto('/sports')
    const sportCards = page.getByTestId('sport-card')
    await expect(sportCards.first()).toBeVisible()
  })

  test('selecting a sport navigates to game selection', async ({ page }) => {
    await page.goto('/sports')
    await page.getByTestId('sport-card').first().click()
    await expect(page).toHaveURL(/\/sports\/\d+\/games/)
    await expect(page.getByRole('heading', { name: 'Select Game' })).toBeVisible()
  })

  test('picking a line navigates to wager entry with the selection listed', async ({ page }) => {
    await page.goto('/sports')
    await page.getByTestId('sport-card').first().click()
    await page.getByTestId('line-button').first().click()

    await expect(page).toHaveURL('/wager/new')
    await expect(page.getByRole('heading', { name: 'Create Wager' })).toBeVisible()
    await expect(page.getByText('Selected Plays')).toBeVisible()
    await expect(page.getByLabel('Customer ID')).toBeVisible()
  })

  test('submitting a valid wager reaches the confirmation step and posts it', async ({ page }) => {
    await page.goto('/sports')
    await page.getByTestId('sport-card').first().click()
    await page.getByTestId('line-button').first().click()

    await page.getByLabel('Customer ID').fill(String(process.env['E2E_CUSTOMER_ID'] ?? '1'))
    await page.getByLabel('Risk Amount ($)').fill('110')
    await page.getByRole('button', { name: 'Review Wager' }).click()

    await expect(page).toHaveURL('/wager/confirm')
    await expect(page.getByRole('heading', { name: 'Confirm Wager' })).toBeVisible()
    await expect(page.getByText('$110.00')).toBeVisible()

    await page.getByRole('button', { name: 'Submit Wager' }).click()

    await expect(page).toHaveURL('/wagers/pending', { timeout: 5000 })
    await expect(page.getByRole('heading', { name: 'Pending Wagers' })).toBeVisible()
    await expect(page.locator('table tbody tr').first()).toBeVisible()
  })

  test('Back button on confirmation returns to wager entry', async ({ page }) => {
    await page.goto('/sports')
    await page.getByTestId('sport-card').first().click()
    await page.getByTestId('line-button').first().click()
    await page.getByLabel('Customer ID').fill(String(process.env['E2E_CUSTOMER_ID'] ?? '1'))
    await page.getByLabel('Risk Amount ($)').fill('110')
    await page.getByRole('button', { name: 'Review Wager' }).click()
    await expect(page).toHaveURL('/wager/confirm')

    await page.getByRole('button', { name: 'Back' }).click()
    await expect(page).toHaveURL('/wager/new')
  })
})

// ── Wager entry — validation ──────────────────────────────────────────────────

test.describe('Wager entry — validation', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page)
  })

  test('navigating directly with no games selected shows an empty-state message', async ({ page }) => {
    await page.goto('/wager/new')
    await expect(page.getByText(/no games selected/i)).toBeVisible()
  })

  test('submit with no customer ID shows validation error', async ({ page }) => {
    await page.goto('/sports')
    await page.getByTestId('sport-card').first().click()
    await page.getByTestId('line-button').first().click()
    await page.getByLabel('Risk Amount ($)').fill('110')
    await page.getByRole('button', { name: 'Review Wager' }).click()
    await expect(page.getByText('Customer ID is required')).toBeVisible()
  })

  test('submit with risk above maximum shows error', async ({ page }) => {
    await page.goto('/sports')
    await page.getByTestId('sport-card').first().click()
    await page.getByTestId('line-button').first().click()
    await page.getByLabel('Customer ID').fill(String(process.env['E2E_CUSTOMER_ID'] ?? '1'))
    await page.getByLabel('Risk Amount ($)').fill('999999')
    await page.getByRole('button', { name: 'Review Wager' }).click()
    await expect(page.getByText(/exceeds maximum/i)).toBeVisible()
  })
})

// ── Pending wagers ─────────────────────────────────────────────────────────────

test.describe('Pending wagers', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page)
  })

  test('pending wagers page shows list', async ({ page }) => {
    await page.goto('/wagers/pending')
    await expect(page.getByRole('heading', { name: /pending wagers/i })).toBeVisible()
  })
})
