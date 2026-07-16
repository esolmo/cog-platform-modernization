# betting-ui

Web betting single-page application for the COG platform. Guides the bettor through Sport Selection → Game Selection → Wager Entry → Confirmation. Replaces both the Classic ASP `Cog-web-betting` pages and the BetMaker Delphi desktop client.

**Dev server:** `http://localhost:5173`  
**Tech:** React 18, TypeScript (strict), Vite, TanStack Query v5, Zustand, React Hook Form, Zod, Tailwind CSS

---

## Pages & Flow

```
/login
  └── /sports                    ← Sport selection grid
        └── /games/:sportId      ← Game list for selected sport
              └── /wager/:gameId ← Wager entry (spread / total / moneyline / parlay)
                    └── /confirmation/:wagerId  ← Ticket confirmation + print
/pending                         ← Pending wagers list with cancel
```

All routes except `/login` require a valid JWT. Unauthenticated requests redirect to `/login`.

---

## Getting Started

### Prerequisites
- Node.js 20+
- npm 10+
- `auth-service` and `betting-service` running locally (or point `VITE_API_BASE_URL` at a remote environment)

### Install & run
```bash
npm install
npm run dev
# App available at http://localhost:5173
```

---

## Environment Variables

Create a `.env.local` file (never commit this file):

```env
VITE_API_BASE_URL=http://localhost:5001
VITE_ALERTS_HUB_URL=http://localhost:5004/hubs/alerts
```

| Variable | Default | Description |
|---|---|---|
| `VITE_API_BASE_URL` | `http://localhost:5001` | Base URL for all API calls (auth + betting services behind ALB in production) |
| `VITE_ALERTS_HUB_URL` | `http://localhost:5004/hubs/alerts` | SignalR hub URL for live odds updates |

In production these are baked into the static bundle at build time via the CI pipeline.

---

## Available Scripts

| Script | Description |
|---|---|
| `npm run dev` | Start Vite dev server with HMR |
| `npm run build` | Type-check + production bundle to `dist/` |
| `npm run preview` | Serve the production build locally |
| `npm test` | Run Vitest unit tests (single run) |
| `npm run test:watch` | Run Vitest in watch mode |
| `npm run test:coverage` | Unit tests with coverage report |
| `npm run test:e2e` | Run Playwright E2E tests |
| `npm run lint` | ESLint (zero warnings policy) |
| `npm run type-check` | TypeScript type check without emitting |

---

## Testing

### Unit tests (Vitest)
```bash
npm test
```

Tests live alongside source files in `src/test/` and co-located `*.test.ts(x)` files. Uses `jsdom` for DOM simulation.

### E2E tests (Playwright)
```bash
# Playwright starts the dev server automatically via webServer config
npm run test:e2e
```

E2E spec: `e2e/betting-flow.spec.ts` (16 tests across 4 describe blocks):
- **Login page** — form elements, empty validation, bad credentials, unauthenticated redirect
- **Betting flow — happy path** — sport selection, game selection, wager entry, confirmation, place-another loop
- **Wager entry — validation** — no bet type, below minimum, above maximum
- **Pending wagers** — list view, cancel flow

#### Running against a specific environment
```bash
BETTING_UI_URL=https://staging.cog.example.com npx playwright test
```

---

## Project Structure

```
src/
├── api/
│   ├── client.ts          # Axios instance (JWT interceptor, 401 → refresh)
│   ├── bettingApi.ts      # Wager + game API functions
│   └── authApi.ts         # Login / refresh API functions
├── components/
│   ├── Layout.tsx          # App shell (nav, auth guard)
│   ├── SportCard.tsx
│   ├── GameRow.tsx
│   └── WagerForm.tsx
├── pages/
│   ├── LoginPage.tsx
│   ├── SportsPage.tsx
│   ├── GamesPage.tsx
│   ├── WagerPage.tsx
│   ├── ConfirmationPage.tsx
│   └── PendingWagersPage.tsx
├── stores/
│   └── authStore.ts        # Zustand store (access token, user info)
├── types/
│   └── betting.ts          # TypeScript interfaces
└── test/
    └── setup.ts            # Vitest global setup
```

---

## Production Build

```bash
npm run build
# Output: dist/
```

The `dist/` folder is deployed to S3 + CloudFront via Terraform (see `terraform/modules/s3-frontend`). All routes fall back to `index.html` via a CloudFront custom error response (SPA routing).

---

## Key Design Decisions

- **TanStack Query** — all server state (games, lines, wager results) is managed via `useQuery` / `useMutation`. No Redux.
- **Zustand** — auth token and user info only. Persisted to `sessionStorage` so a page refresh keeps the user logged in.
- **Axios interceptor** — transparently refreshes the access token on 401 responses and retries the original request.
- **`data-testid` attributes** — all interactive elements used in E2E tests carry `data-testid` (e.g., `sport-card`, `game-row`, `bet-type-spread`, `win-amount`). Do not remove these.
- **Strict TypeScript** — `"strict": true` in `tsconfig.json`. No `any` types.
- **Real-time odds** — `@microsoft/signalr` connects to `alerts-service` hub on mount. Odds updates invalidate the relevant TanStack Query cache entry automatically.
