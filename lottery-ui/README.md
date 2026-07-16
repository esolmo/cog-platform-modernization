# lottery-ui

Pick3 and Pick4 lottery ticket purchasing single-page application. Replaces the `Lottery` ASP.NET WebForms application.

**Dev server:** `http://localhost:5176`  
**Tech:** React 18, TypeScript (strict), Vite, TanStack Query v5, Zustand, React Hook Form, Zod, Tailwind CSS

---

## Pages

| Route | Page | Description |
|---|---|---|
| `/login` | LoginPage | JWT auth |
| `/games` | GamesPage | Pick3 / Pick4 game selection |
| `/pick/:drawingId` | PickPage | Number selection + pick type (straight/boxed) + amount |
| `/history` | HistoryPage | Customer's past tickets |
| `/tickets/:id` | TicketDetailPage | Ticket detail with picks and status |

---

## Getting Started

```bash
npm install
npm run dev
# http://localhost:5176
```

### Prerequisites
- `auth-service` on port 5001
- `lottery-service` on port 5006

---

## Environment Variables

`.env.local`:
```env
VITE_API_BASE_URL=http://localhost:5001
VITE_LOTTERY_SERVICE_URL=http://localhost:5006
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

## Production Build

```bash
npm run build
# dist/ deployed to S3 + CloudFront
```
