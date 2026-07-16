import { test, expect, Page } from '@playwright/test'

// ── Helpers ────────────────────────────────────────────────────────────────────

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

// ── Full betting flow: Login → Sport → Game → Wager → Confirmation ────────────

test.describe('Betting flow — happy path', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page)
  })

  test('lands on sport selection after login', async ({ page }) => {
    await expect(page).toHaveURL('/sports')
    await expect(page.getByRole('heading', { name: /select sport/i })).toBeVisible()
  })

  test('sport cards are listed', async ({ page }) => {
    await page.goto('/sports')
    const sportCards = page.getByTestId('sport-card')
    await expect(sportCards.first()).toBeVisible()
  })

  test('selecting a sport navigates to game selection', async ({ page }) => {
    await page.goto('/sports')
    await page.getByTestId('sport-card').first().click()
    await expect(page).toHaveURL(/\/games/)
    await expect(page.getByRole('heading', { name: /select game/i })).toBeVisible()
  })

  test('selecting a game navigates to wager entry', async ({ page }) => {
    await page.goto('/sports')
    await page.getByTestId('sport-card').first().click()
    await page.getByTestId('game-row').first().click()
    await expect(page).toHaveURL(/\/wager/)
    await expect(page.getByRole('heading', { name: /wager entry/i })).toBeVisible()
  })

  test('wager entry — spread bet — shows risk and win amounts', async ({ page }) => {
    await page.goto('/sports')
    await page.getByTestId('sport-card').first().click()
    await page.getByTestId('game-row').first().click()

    // Select spread bet on home team
    await page.getByTestId('bet-type-spread').click()
    await page.getByTestId('side-home').click()
    await page.getByLabel('Risk Amount').fill('110')

    // Win amount should auto-calculate (standard -110 juice = win 100)
    await expect(page.getByTestId('win-amount')).toContainText('100')
  })

  test('wager entry — submitting valid wager navigates to confirmation', async ({ page }) => {
    await page.goto('/sports')
    await page.getByTestId('sport-card').first().click()
    await page.getByTestId('game-row').first().click()

    await page.getByTestId('bet-type-spread').click()
    await page.getByTestId('side-home').click()
    await page.getByLabel('Risk Amount').fill('110')
    await page.getByRole('button', { name: /submit wager/i }).click()

    await expect(page).toHaveURL(/\/confirmation/, { timeout: 5000 })
    await expect(page.getByRole('heading', { name: /wager confirmed/i })).toBeVisible()
    await expect(page.getByTestId('ticket-number')).toBeVisible()
  })

  test('confirmation page — place another wager returns to sport selection', async ({
    page,
  }) => {
    await page.goto('/sports')
    await page.getByTestId('sport-card').first().click()
    await page.getByTestId('game-row').first().click()
    await page.getByTestId('bet-type-spread').click()
    await page.getByTestId('side-home').click()
    await page.getByLabel('Risk Amount').fill('110')
    await page.getByRole('button', { name: /submit wager/i }).click()
    await expect(page).toHaveURL(/\/confirmation/, { timeout: 5000 })

    await page.getByRole('button', { name: /place another/i }).click()
    await expect(page).toHaveURL('/sports')
  })
})

// ── Wager entry — validation ──────────────────────────────────────────────────

test.describe('Wager entry — validation', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page)
    await page.goto('/sports')
    await page.getByTestId('sport-card').first().click()
    await page.getByTestId('game-row').first().click()
  })

  test('submit with no bet type selected shows error', async ({ page }) => {
    await page.getByRole('button', { name: /submit wager/i }).click()
    await expect(page.getByText(/select a bet type/i)).toBeVisible()
  })

  test('submit with risk below minimum shows error', async ({ page }) => {
    await page.getByTestId('bet-type-spread').click()
    await page.getByTestId('side-home').click()
    await page.getByLabel('Risk Amount').fill('1')
    await page.getByRole('button', { name: /submit wager/i }).click()
    await expect(page.getByText(/minimum wager/i)).toBeVisible()
  })

  test('submit with risk above maximum shows error', async ({ page }) => {
    await page.getByTestId('bet-type-spread').click()
    await page.getByTestId('side-home').click()
    await page.getByLabel('Risk Amount').fill('999999')
    await page.getByRole('button', { name: /submit wager/i }).click()
    await expect(page.getByText(/exceeds maximum/i)).toBeVisible()
  })
})

// ── Pending wagers ─────────────────────────────────────────────────────────────

test.describe('Pending wagers', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page)
  })

  test('pending wagers page shows list', async ({ page }) => {
    await page.goto('/pending')
    await expect(page.getByRole('heading', { name: /pending wagers/i })).toBeVisible()
  })

  test('can cancel a pending wager', async ({ page }) => {
    // Place a wager first
    await page.goto('/sports')
    await page.getByTestId('sport-card').first().click()
    await page.getByTestId('game-row').first().click()
    await page.getByTestId('bet-type-spread').click()
    await page.getByTestId('side-home').click()
    await page.getByLabel('Risk Amount').fill('110')
    await page.getByRole('button', { name: /submit wager/i }).click()
    await expect(page).toHaveURL(/\/confirmation/, { timeout: 5000 })

    // View pending wagers and cancel
    await page.goto('/pending')
    const cancelBtn = page.getByRole('button', { name: /cancel/i }).first()
    await expect(cancelBtn).toBeVisible()
    await cancelBtn.click()
    await page.getByRole('button', { name: /confirm/i }).click()
    await expect(page.getByText(/cancelled/i)).toBeVisible()
  })
})
