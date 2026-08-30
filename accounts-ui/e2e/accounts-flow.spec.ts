import { test, expect, Page } from '@playwright/test'

// ── Helpers ────────────────────────────────────────────────────────────────────

async function loginAs(page: Page, loginName = 'agent1', password = 'P@ssw0rd!') {
  await page.goto('/login')
  await page.getByLabel('Login Name').fill(loginName)
  await page.getByLabel('Password').fill(password)
  await page.getByRole('button', { name: 'Sign In' }).click()
  await expect(page).not.toHaveURL('/login', { timeout: 5000 })
}

// ── Login page ─────────────────────────────────────────────────────────────────

test.describe('Login page', () => {
  test('renders all form elements', async ({ page }) => {
    await page.goto('/login')
    await expect(page.getByRole('heading', { name: 'COG Accounts' })).toBeVisible()
    await expect(page.getByLabel('Login Name')).toBeVisible()
    await expect(page.getByLabel('Password')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Sign In' })).toBeVisible()
  })

  test('validates empty submission', async ({ page }) => {
    await page.goto('/login')
    await page.getByRole('button', { name: 'Sign In' }).click()
    await expect(page.getByText('Login name is required')).toBeVisible()
    await expect(page.getByText('Password is required')).toBeVisible()
  })

  test('shows error for bad credentials', async ({ page }) => {
    await page.goto('/login')
    await page.getByLabel('Login Name').fill('nobody')
    await page.getByLabel('Password').fill('wrongpass')
    await page.getByRole('button', { name: 'Sign In' }).click()
    await expect(page.getByText(/invalid credentials/i)).toBeVisible({ timeout: 5000 })
  })

  test('redirects unauthenticated user to login', async ({ page }) => {
    await page.goto('/customers')
    await expect(page).toHaveURL('/login')
  })
})

// ── Customer list ──────────────────────────────────────────────────────────────

test.describe('Customer list', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page)
  })

  test('lands on customer list after login', async ({ page }) => {
    await expect(page).toHaveURL('/customers')
    await expect(page.getByRole('heading', { name: 'Customers' })).toBeVisible()
  })

  test('shows customer table with at least one row', async ({ page }) => {
    await page.goto('/customers')
    await expect(page.locator('table tbody tr').first()).toBeVisible({ timeout: 5000 })
  })

  test('New Customer button navigates to create form', async ({ page }) => {
    await page.goto('/customers')
    await page.getByRole('link', { name: 'New Customer' }).click()
    await expect(page).toHaveURL('/customers/new')
    await expect(page.getByRole('heading', { name: 'New Customer' })).toBeVisible()
  })

  test('View link navigates to customer dashboard', async ({ page }) => {
    await page.goto('/customers')
    await page.getByRole('link', { name: 'View' }).first().click()
    await expect(page).toHaveURL(/\/customers\/\d+/)
  })
})

// ── Customer dashboard ─────────────────────────────────────────────────────────

test.describe('Customer dashboard', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page)
    await page.goto('/customers')
    await page.getByRole('link', { name: 'View' }).first().click()
    await expect(page).toHaveURL(/\/customers\/\d+/)
  })

  test('shows customer login name and status', async ({ page }) => {
    // Header area has customer login name
    await expect(page.locator('h1').first()).toBeVisible()
    // Status badge (Active/Inactive/Suspended) is rendered
    await expect(
      page.locator('span').filter({ hasText: /^(Active|Inactive|Suspended)$/ }).first()
    ).toBeVisible()
  })

  test('shows four balance summary cards', async ({ page }) => {
    await expect(page.getByText('Credit Limit')).toBeVisible()
    await expect(page.getByText('Current Balance')).toBeVisible()
    await expect(page.getByText('Available')).toBeVisible()
    await expect(page.locator('p', { hasText: 'Free Play' })).toBeVisible()
  })

  test('Personal tab is active by default', async ({ page }) => {
    // Personal tab button should have the active border style
    const personalTab = page.getByRole('button', { name: 'Personal' })
    await expect(personalTab).toBeVisible()
    await expect(personalTab).toHaveClass(/border-blue-600/)
  })

  test('clicking Limits tab shows limits content', async ({ page }) => {
    await page.getByRole('button', { name: 'Limits' }).click()
    // LimitsTab renders at least a section heading or wager limit label
    await expect(page.getByText('Wager Limit', { exact: true })).toBeVisible({ timeout: 3000 })
  })

  test('clicking Transactions tab shows transaction form', async ({ page }) => {
    await page.getByRole('button', { name: 'Transactions' }).click()
    await expect(page.getByRole('heading', { name: 'New Transaction' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Post Transaction' })).toBeVisible()
  })
})

// ── Transaction creation ───────────────────────────────────────────────────────

test.describe('Transaction creation', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page)
    await page.goto('/customers')
    await page.getByRole('link', { name: 'View' }).first().click()
    await expect(page).toHaveURL(/\/customers\/\d+/)
    await page.getByRole('button', { name: 'Transactions' }).click()
    await expect(page.getByRole('heading', { name: 'New Transaction' })).toBeVisible()
  })

  test('submit with zero amount shows validation error', async ({ page }) => {
    await page.getByRole('button', { name: 'Post Transaction' }).click()
    await expect(page.getByText('Amount must be positive')).toBeVisible()
  })

  test('posting a credit transaction succeeds', async ({ page }) => {
    // Record balance before
    const balanceBefore = await page
      .getByText('Current Balance')
      .locator('..')
      .locator('p.text-lg')
      .textContent()

    // Fill transaction form
    await page.getByLabel('Code').selectOption('Credit')
    await page.getByLabel('Type').selectOption('Cash')
    await page.getByLabel('Amount').fill('100')
    await page.getByLabel('Description').fill('E2E test deposit')
    await page.getByRole('button', { name: 'Post Transaction' }).click()

    // Form resets on success (amount back to 0 or empty) and history table updates
    await expect(page.getByText('E2E test deposit')).toBeVisible({ timeout: 5000 })

    // Balance summary card should have updated
    const balanceAfter = await page
      .getByText('Current Balance')
      .locator('..')
      .locator('p.text-lg')
      .textContent()

    expect(balanceAfter).not.toBe(balanceBefore)
  })

  test('posting a debit transaction appears in history with red badge', async ({ page }) => {
    await page.getByLabel('Code').selectOption('Debit')
    await page.getByLabel('Type').selectOption('Cash')
    await page.getByLabel('Amount').fill('50')
    await page.getByLabel('Description').fill('E2E test withdrawal')
    await page.getByRole('button', { name: 'Post Transaction' }).click()

    await expect(page.getByText('E2E test withdrawal')).toBeVisible({ timeout: 5000 })
    // Debit badge has red styling
    await expect(page.locator('span').filter({ hasText: 'Debit' }).first()).toHaveClass(/bg-red/)
  })

  test('transaction history table shows posted entries', async ({ page }) => {
    // Table headers are visible when transactions tab is open
    await expect(page.getByRole('columnheader', { name: 'Date' })).toBeVisible()
    await expect(page.getByRole('columnheader', { name: 'Amount' })).toBeVisible()
    await expect(page.getByRole('columnheader', { name: 'Balance After' })).toBeVisible()
  })
})

// ── Create new customer ────────────────────────────────────────────────────────

test.describe('Create new customer', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page)
    await page.goto('/customers/new')
    await expect(page.getByRole('heading', { name: 'New Customer' })).toBeVisible()
  })

  test('renders Personal Information and Limits sections', async ({ page }) => {
    await expect(page.getByText('Personal Information')).toBeVisible()
    await expect(page.getByText('Limits')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Create Customer' })).toBeVisible()
  })

  test('validates missing login name', async ({ page }) => {
    await page.getByRole('button', { name: 'Create Customer' }).click()
    // Login Name field is required (min length 1)
    await expect(page.getByText(/string must contain at least 1/i)).toBeVisible()
  })

  test('Cancel returns to customer list', async ({ page }) => {
    await page.getByRole('button', { name: 'Cancel' }).click()
    await expect(page).toHaveURL('/customers')
  })

  test('creates a customer and navigates to their dashboard', async ({ page }) => {
    const uniqueName = `e2e${Date.now().toString().slice(-6)}`

    await page.getByLabel('Login Name *').fill(uniqueName)
    await page.getByLabel('Odds Format').selectOption('American')
    // Numeric fields have defaults; submit as-is
    await page.getByRole('button', { name: 'Create Customer' }).click()

    // Navigates to the new customer's dashboard
    await expect(page).toHaveURL(/\/customers\/\d+/, { timeout: 5000 })
    await expect(page.locator('h1').filter({ hasText: uniqueName })).toBeVisible()
  })
})
