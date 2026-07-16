# casino-ui

Live Dealer casino single-page application for the COG platform. Provides customers with a balance dashboard, chip transfer (deposit/withdraw), and a lobby link that launches the external Live Dealer provider. Replaces the `Cog-web-betting/crazyhorse/` Classic ASP pages.

**Dev server:** `http://localhost:5178`  
**Tech:** React 18, TypeScript (strict), Vite, TanStack Query v5, Zustand, React Hook Form, Zod, Tailwind CSS

---

## Pages & Flow

```
/login          ← JWT auth (posts to auth-service)
/setup          ← Nickname registration (first visit only)
/casino         ← Lobby: balance cards + transfer modal + Play Now link
```

- `/casino` redirects to `/setup` when the casino session returns 404 (player not registered yet).
- All routes except `/login` require a valid JWT.

---

## Getting Started

### Prerequisites
- Node.js 20+
- npm 10+
- `auth-service` running on port 5010
- `casino-service` running on port 5080

### Install & run
```bash
npm install
npm run dev
# App available at http://localhost:5178
```

---

## Environment Variables

Create a `.env.local` file if you need to override the default Vite proxy targets:

```env
# No required env vars for local dev — proxy in vite.config.ts handles routing.
# For production builds, API requests go to the same origin (served behind a reverse proxy).
```

---

## Available Scripts

| Script | Description |
|---|---|
| `npm run dev` | Start Vite dev server on port 5178 |
| `npm run build` | Type-check + production bundle to `dist/` |
| `npm run preview` | Serve production build locally |
| `npm test` | Run Vitest unit tests |
| `npm run test:watch` | Vitest in watch mode |
| `npm run test:coverage` | Unit tests with coverage report |
| `npm run lint` | ESLint (zero warnings policy) |
| `npm run type-check` | TypeScript type check |

---

## Testing

```bash
npm test
```

Unit tests (11 total across 3 describe blocks):
- **LoginPage** — renders form, validates empty submit, calls auth API on valid input
- **SetupPage** — renders form, disables submit until 2+ chars, calls register API
- **CasinoLobbyPage** — shows balance cards, opens transfer modal, redirects to `/setup` on 404

---

## Project Structure

```
src/
├── api/
│   ├── client.ts          # Axios instance (JWT interceptor, 401 → clearAuth)
│   └── casinoApi.ts       # Session, balance, register, transfer API functions
├── pages/
│   ├── LoginPage.tsx
│   ├── SetupPage.tsx      # Nickname registration
│   └── CasinoLobbyPage.tsx
├── store/
│   └── authStore.ts       # Zustand (accessToken, loginName, customerId)
├── App.tsx                # Routes + RequireAuth guard
├── main.tsx               # Entry point (QueryClientProvider + BrowserRouter)
└── test/
    └── setup.ts           # @testing-library/jest-dom
```

---

## Vite Proxy (local dev)

| Path prefix | Target | Purpose |
|---|---|---|
| `/api` | `http://localhost:5080` | casino-service (balance, session, transfer) |

Auth calls (`/api/auth/login`) are also proxied to port 5080, which forwards to auth-service internally — or override by splitting the proxy if auth-service is run standalone.

---

## Production Build

```bash
npm run build
# Output: dist/ → deployed to S3 + CloudFront via Terraform
```

---

## Key Design Decisions

- **Redirect-to-setup on 404** — `CasinoLobbyPage` catches a 404 on the session query and navigates to `/setup` rather than showing an error, giving new players a smooth onboarding path.
- **60-second balance poll** — TanStack Query `refetchInterval: 60_000` keeps balances reasonably fresh without hammering the backend.
- **Play Now link** — opens `session.lobbyUrl` in a new tab (`target="_blank" rel="noopener noreferrer"`); the URL is provided by casino-service and contains a short-lived token issued by the Live Dealer provider.
- **Zustand auth store** — persists `accessToken`, `loginName`, and `customerId` in memory only (no localStorage); session is lost on page refresh, forcing re-login for security.
