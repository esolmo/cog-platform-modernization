import { test, expect, Page } from '@playwright/test'

// ── Helpers ────────────────────────────────────────────────────────────────────

async function loginAs(page: Page, username = 'admin', password = 'Admin123!') {
  await page.goto('/login')
  await page.getByLabel('Username').fill(username)
  await page.getByLabel('Password').fill(password)
  await page.getByRole('button', { name: 'Sign in' }).click()
  await expect(page).not.toHaveURL('/login', { timeout: 5000 })
}

function uniqueSuffix() {
  return Date.now().toString().slice(-8)
}

// ── Login page ─────────────────────────────────────────────────────────────────

test.describe('Login page', () => {
  test('renders all form elements', async ({ page }) => {
    await page.goto('/login')
    await expect(page.getByRole('heading', { name: 'COG Admin' })).toBeVisible()
    await expect(page.getByLabel('Username')).toBeVisible()
    await expect(page.getByLabel('Password')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Sign in' })).toBeVisible()
  })

  test('validates empty submission', async ({ page }) => {
    await page.goto('/login')
    await page.getByRole('button', { name: 'Sign in' }).click()
    await expect(page.getByText('Username is required')).toBeVisible()
    await expect(page.getByText('Password is required')).toBeVisible()
  })

  test('shows error for bad credentials', async ({ page }) => {
    await page.goto('/login')
    await page.getByLabel('Username').fill('nobody')
    await page.getByLabel('Password').fill('wrongpass')
    await page.getByRole('button', { name: 'Sign in' }).click()
    await expect(page.getByText(/invalid username or password/i)).toBeVisible({ timeout: 5000 })
  })

  test('redirects unauthenticated user to login', async ({ page }) => {
    await page.goto('/users')
    await expect(page).toHaveURL('/login')
  })
})

// ── Navigation ───────────────────────────────────────────────────────────────

test.describe('Navigation', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page)
  })

  test('lands on Users after login', async ({ page }) => {
    await expect(page).toHaveURL('/users')
    await expect(page.getByRole('heading', { name: 'Users' })).toBeVisible()
  })

  test('sidebar links navigate between the four admin pages', async ({ page }) => {
    await page.getByRole('link', { name: 'Roles & Permissions' }).click()
    await expect(page).toHaveURL('/roles')
    await expect(page.getByRole('heading', { name: 'Roles & Permissions' })).toBeVisible()

    await page.getByRole('link', { name: 'System Config' }).click()
    await expect(page).toHaveURL('/config')
    await expect(page.getByRole('heading', { name: 'System Configuration' })).toBeVisible()

    await page.getByRole('link', { name: 'Audit Log' }).click()
    await expect(page).toHaveURL('/audit')
    await expect(page.getByRole('heading', { name: 'Audit Log' })).toBeVisible()
  })

  test('Sign out returns to login and blocks further access', async ({ page }) => {
    await page.getByRole('button', { name: 'Sign out' }).click()
    await expect(page).toHaveURL('/login')
    await page.goto('/users')
    await expect(page).toHaveURL('/login')
  })
})

// ── Users page ─────────────────────────────────────────────────────────────────

test.describe('Users page', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page)
  })

  test('renders the users table with the expected columns', async ({ page }) => {
    // NOTE: the seeded "admin" login used to sign in here lives only in auth-service's
    // own Users table (via its SeedAdminUser migration) — it is deliberately *not* also
    // an admin-service ApplicationUsers record unless someone provisions one through this
    // very page, so it correctly never appears in this table on its own.
    await expect(page.getByRole('columnheader', { name: 'Username' })).toBeVisible()
    await expect(page.getByRole('columnheader', { name: 'Email' })).toBeVisible()
    await expect(page.getByRole('columnheader', { name: 'Roles' })).toBeVisible()
    await expect(page.getByRole('columnheader', { name: 'Status' })).toBeVisible()
  })

  test('New User modal opens and closes via Cancel', async ({ page }) => {
    await page.getByRole('button', { name: '+ New User' }).click()
    await expect(page.getByRole('heading', { name: 'New User' })).toBeVisible()

    await page.getByRole('button', { name: 'Cancel' }).click()
    await expect(page.getByRole('heading', { name: 'New User' })).not.toBeVisible()
  })

  test('creates a new user and it appears in the table', async ({ page }) => {
    const username = `e2euser${uniqueSuffix()}`

    await page.getByRole('button', { name: '+ New User' }).click()
    await page.getByLabel('Username').fill(username)
    await page.getByLabel('Password').fill('P@ssw0rd123!')
    await page.getByLabel('First name').fill('E2E')
    await page.getByLabel('Last name').fill('Tester')
    await page.getByLabel('Email').fill(`${username}@cog.local`)
    await page.getByRole('button', { name: 'Create user' }).click()

    await expect(page.getByRole('heading', { name: 'New User' })).not.toBeVisible({ timeout: 5000 })
    await expect(page.getByRole('cell', { name: username, exact: true })).toBeVisible()
  })

  test('validates a too-short password on create', async ({ page }) => {
    await page.getByRole('button', { name: '+ New User' }).click()
    await page.getByLabel('Username').fill(`shortpw${uniqueSuffix()}`)
    await page.getByLabel('Password').fill('short')
    await page.getByLabel('First name').fill('E2E')
    await page.getByLabel('Last name').fill('Tester')
    await page.getByLabel('Email').fill('shortpw@cog.local')
    await page.getByRole('button', { name: 'Create user' }).click()

    await expect(page.getByText('Min 8 characters')).toBeVisible()
    await expect(page.getByRole('heading', { name: 'New User' })).toBeVisible()
  })

  test('search filters the user list', async ({ page }) => {
    const username = `findme${uniqueSuffix()}`
    await page.getByRole('button', { name: '+ New User' }).click()
    await page.getByLabel('Username').fill(username)
    await page.getByLabel('Password').fill('P@ssw0rd123!')
    await page.getByLabel('First name').fill('Find')
    await page.getByLabel('Last name').fill('Me')
    await page.getByLabel('Email').fill(`${username}@cog.local`)
    await page.getByRole('button', { name: 'Create user' }).click()
    await expect(page.getByRole('cell', { name: username, exact: true })).toBeVisible({ timeout: 5000 })

    await page.getByPlaceholder('Search by username, email, or name...').fill(username)
    await expect(page.getByRole('cell', { name: username, exact: true })).toBeVisible()
    await expect(page.getByRole('cell', { name: 'admin', exact: true })).not.toBeVisible()
  })

  test('editing a user updates their name in the table', async ({ page }) => {
    const username = `edituser${uniqueSuffix()}`
    await page.getByRole('button', { name: '+ New User' }).click()
    await page.getByLabel('Username').fill(username)
    await page.getByLabel('Password').fill('P@ssw0rd123!')
    await page.getByLabel('First name').fill('Before')
    await page.getByLabel('Last name').fill('Edit')
    await page.getByLabel('Email').fill(`${username}@cog.local`)
    await page.getByRole('button', { name: 'Create user' }).click()
    const row = page.getByRole('row').filter({ hasText: username })
    await expect(row).toBeVisible({ timeout: 5000 })

    await row.getByRole('button', { name: 'Edit' }).click()
    await expect(page.getByRole('heading', { name: 'Edit User' })).toBeVisible()
    await page.getByLabel('First name').fill('After')
    await page.getByRole('button', { name: 'Save changes' }).click()

    await expect(page.getByRole('heading', { name: 'Edit User' })).not.toBeVisible({ timeout: 5000 })
    await expect(page.getByRole('row').filter({ hasText: username })).toContainText('After')
  })
})

// ── Roles page ─────────────────────────────────────────────────────────────────

test.describe('Roles page', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page)
    await page.getByRole('link', { name: 'Roles & Permissions' }).click()
    await expect(page).toHaveURL('/roles')
  })

  test('shows the seeded system roles', async ({ page }) => {
    await expect(page.getByText('Admin', { exact: true })).toBeVisible()
  })

  test('expanding a role shows its permissions', async ({ page }) => {
    await page.getByText('Admin', { exact: true }).click()
    await expect(page.getByText('Administrative access', { exact: true })).toBeVisible()
  })

  test('creates a new role with selected permissions', async ({ page }) => {
    const roleName = `E2ERole${uniqueSuffix()}`

    await page.getByRole('button', { name: '+ New Role' }).click()
    await page.getByLabel('Role name').fill(roleName)
    await page.getByLabel('Description').fill('Created by E2E test')
    const firstPermission = page.locator('label').filter({ has: page.locator('input[type="checkbox"]') }).first()
    await firstPermission.locator('input[type="checkbox"]').check()
    await page.getByRole('button', { name: 'Create role' }).click()

    await expect(page.getByRole('heading', { name: 'New Role' })).not.toBeVisible({ timeout: 5000 })
    await expect(page.getByText(roleName, { exact: true })).toBeVisible()
  })
})

// ── System Config page ───────────────────────────────────────────────────────

test.describe('System Config page', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page)
    await page.getByRole('link', { name: 'System Config' }).click()
    await expect(page).toHaveURL('/config')
  })

  test('adds a new setting and it appears in the table', async ({ page }) => {
    const key = `e2e.setting.${uniqueSuffix()}`

    await page.getByRole('button', { name: '+ Add Setting' }).click()
    await page.getByLabel('Key').fill(key)
    await page.getByLabel('Value', { exact: false }).fill('test-value')
    await page.getByLabel('Category').fill('E2E')
    await page.getByLabel('Description').fill('Added by E2E test')
    await page.getByRole('button', { name: 'Save' }).click()

    await expect(page.getByRole('heading', { name: 'New Setting' })).not.toBeVisible({ timeout: 5000 })
    await expect(page.getByText(key, { exact: true })).toBeVisible()
  })

  test('editing a setting updates its value', async ({ page }) => {
    const key = `e2e.edit.${uniqueSuffix()}`
    await page.getByRole('button', { name: '+ Add Setting' }).click()
    await page.getByLabel('Key').fill(key)
    await page.getByLabel('Value', { exact: false }).fill('original')
    await page.getByLabel('Category').fill('E2E')
    await page.getByLabel('Description').fill('desc')
    await page.getByRole('button', { name: 'Save' }).click()
    const row = page.getByRole('row').filter({ hasText: key })
    await expect(row).toBeVisible({ timeout: 5000 })

    await row.getByRole('button', { name: 'Edit' }).click()
    await expect(page.getByRole('heading', { name: 'Edit Setting' })).toBeVisible()
    await page.getByLabel('Value', { exact: false }).fill('updated')
    await page.getByRole('button', { name: 'Save' }).click()

    await expect(page.getByRole('heading', { name: 'Edit Setting' })).not.toBeVisible({ timeout: 5000 })
    await expect(page.getByRole('row').filter({ hasText: key })).toContainText('updated')
  })

  test('deletes a setting', async ({ page }) => {
    const key = `e2e.delete.${uniqueSuffix()}`
    await page.getByRole('button', { name: '+ Add Setting' }).click()
    await page.getByLabel('Key').fill(key)
    await page.getByLabel('Value', { exact: false }).fill('to-delete')
    await page.getByLabel('Category').fill('E2E')
    await page.getByLabel('Description').fill('desc')
    await page.getByRole('button', { name: 'Save' }).click()
    const row = page.getByRole('row').filter({ hasText: key })
    await expect(row).toBeVisible({ timeout: 5000 })

    page.once('dialog', (dialog) => dialog.accept())
    await row.getByRole('button', { name: 'Delete' }).click()

    await expect(page.getByText(key, { exact: true })).not.toBeVisible({ timeout: 5000 })
  })
})

// ── Audit Log page ───────────────────────────────────────────────────────────

test.describe('Audit Log page', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page)
  })

  test('shows entries after performing an auditable action', async ({ page }) => {
    // Create a config setting first so there's guaranteed to be at least one audit entry.
    await page.getByRole('link', { name: 'System Config' }).click()
    const key = `e2e.audit.${uniqueSuffix()}`
    await page.getByRole('button', { name: '+ Add Setting' }).click()
    await page.getByLabel('Key').fill(key)
    await page.getByLabel('Value', { exact: false }).fill('v')
    await page.getByLabel('Category').fill('E2E')
    await page.getByLabel('Description').fill('d')
    await page.getByRole('button', { name: 'Save' }).click()
    await expect(page.getByText(key, { exact: true })).toBeVisible({ timeout: 5000 })

    await page.getByRole('link', { name: 'Audit Log' }).click()
    await expect(page).toHaveURL('/audit')
    await expect(page.getByRole('columnheader', { name: 'Action' })).toBeVisible()
    await expect(page.locator('table tbody tr').first()).toBeVisible()
  })
})
