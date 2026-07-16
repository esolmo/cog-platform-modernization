# reports-ui

Reporting dashboard single-page application for the COG platform. Displays wager activity, transaction, and agent performance reports with sortable tables and charts. Replaces the `WebReports` ASP.NET WebForms application (Telerik RadControls).

**Dev server:** `http://localhost:5177`  
**Tech:** React 18, TypeScript (strict), Vite, TanStack Query v5, TanStack Table v8, Recharts, Tailwind CSS

---

## Pages

| Route | Page | Description |
|---|---|---|
| `/login` | LoginPage | JWT auth |
| `/reports/wagers` | WagersReport | Wager activity — filterable by agent, date range |
| `/reports/transactions` | TransactionsReport | Transaction report — credits, debits, net |
| `/reports/agents` | AgentsReport | Agent performance — volume, hold%, positions |

---

## Getting Started

```bash
npm install
npm run dev
# http://localhost:5177
```

### Prerequisites
- `auth-service` on port 5001
- `reports-service` on port 5007

---

## Environment Variables

`.env.local`:
```env
VITE_API_BASE_URL=http://localhost:5001
VITE_REPORTS_SERVICE_URL=http://localhost:5007
```

---

## Available Scripts

| Script | Description |
|---|---|
| `npm run dev` | Start dev server |
| `npm run build` | Production build |
| `npm test` | Vitest unit tests |
| `npm run test:coverage` | Tests with coverage |
| `npm run lint` | ESLint |

---

## Key Libraries

| Library | Use |
|---|---|
| TanStack Table v8 | Sortable, paginated data tables with column filtering |
| Recharts | Bar/line charts for volume trends |
| date-fns | Date range picker formatting |

---

## Production Build

```bash
npm run build
# dist/ deployed to S3 + CloudFront
```
