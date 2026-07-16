# accounts-ui

Customer account management single-page application for the COG platform. Provides agents with a full customer dashboard: personal info, credit limits, transaction history, and new transaction entry. Replaces the Accounts Delphi desktop (56 forms) and the `Cog-web-betting/Accounts/` ASP pages.

**Dev server:** `http://localhost:5174`  
**Tech:** React 18, TypeScript (strict), Vite, TanStack Query v5, Zustand, React Hook Form, Zod, Tailwind CSS

---

## Pages & Flow

```
/login
  └── /customers                   ← Paginated customer list with status/balance columns
        ├── /customers/new         ← Create new customer form (personal info + limits)
        └── /customers/:id         ← Customer dashboard
              ├── Personal tab     ← Login name, contact info, odds format
              ├── Limits tab       ← Wager limits, credit limit, hard limit
              └── Transactions tab ← Transaction history + new transaction form
/agents                            ← Agent hierarchy tree
```

All routes except `/login` require a valid JWT.

---

## Getting Started

### Prerequisites
- Node.js 20+
- npm 10+
- `auth-service` and `accounts-service` running locally

### Install & run
```bash
npm install
npm run dev
# App available at http://localhost:5174
```

---

## Environment Variables

Create a `.env.local` file:

```env
VITE_API_BASE_URL=http://localhost:5001
```

| Variable | Default | Description |
|---|---|---|
| `VITE_API_BASE_URL` | `http://localhost:5001` | Base URL for all API requests (auth + accounts services) |

---

## Available Scripts

| Script | Description |
|---|---|
| `npm run dev` | Start Vite dev server |
| `npm run build` | Type-check + production bundle to `dist/` |
| `npm run preview` | Serve production build locally |
| `npm test` | Run Vitest unit tests |
| `npm run test:watch` | Vitest in watch mode |
| `npm run test:coverage` | Unit tests with coverage report |
| `npm run test:e2e` | Run Playwright E2E tests |
| `npm run lint` | ESLint (zero warnings policy) |
| `npm run type-check` | TypeScript type check |

---

## Testing

### Unit tests (Vitest)
```bash
npm test
```

### E2E tests (Playwright)
```bash
npm run test:e2e
```

Playwright config: `playwright.config.ts` (base URL `http://localhost:5174`, Chromium only).

E2E spec: `e2e/accounts-flow.spec.ts` (16 tests across 5 describe blocks):
- **Login page** — form elements, empty validation, bad credentials, unauthenticated redirect
- **Customer list** — lands on `/customers` after login, table rows, New Customer nav, View link
- **Customer dashboard** — status badge, balance cards, tab navigation
- **Transaction creation** — zero-amount validation, credit/debit posting, history table
- **Create new customer** — section rendering, login name validation, cancel, full create flow

#### Running against a specific environment
```bash
ACCOUNTS_UI_URL=https://staging.cog.example.com npx playwright test
```

---

## Project Structure

```
src/
├── api/
│   ├── client.ts            # Axios instance (JWT interceptor, 401 → refresh)
│   └── accountsApi.ts       # Customer, transaction, agent API functions
├── components/
│   └── Layout.tsx            # App shell + auth guard
├── pages/
│   ├── LoginPage.tsx
│   ├── CustomerListPage.tsx
│   ├── CustomerDashboardPage.tsx
│   ├── CreateCustomerPage.tsx
│   ├── AgentHierarchyPage.tsx
│   └── customer-tabs/
│       ├── PersonalTab.tsx
│       ├── LimitsTab.tsx
│       └── TransactionsTab.tsx
├── stores/
│   └── authStore.ts          # Zustand (access token, agentId, loginName)
└── types/
    └── accounts.ts           # TypeScript interfaces
```

---

## Production Build

```bash
npm run build
# Output: dist/  → deployed to S3 + CloudFront
```

---

## Key Design Decisions

- **TanStack Query** — customer list, dashboard data, and transaction history are all server state. The `transactions` query is only enabled when the Transactions tab is active (lazy loading).
- **Optimistic balance update** — after `POST /api/transactions` succeeds, TanStack Query invalidates both the `['transactions', customerId]` and `['customer', customerId]` cache keys so the balance cards and history table refresh automatically.
- **Zod validation** — form validation schemas mirror the API request contracts; errors surface immediately client-side before a network call is made.
- **`data-testid` attributes** — interactive elements used in E2E tests carry `data-testid`. Do not remove these.
