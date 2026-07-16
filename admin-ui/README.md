# admin-ui

System administration single-page application for the COG platform. Provides user and role management, system configuration editing, and audit log access. Replaces the Admin Delphi desktop (4 main forms) and the LoginsRoles Delphi desktop (9 forms).

**Dev server:** `http://localhost:5175`  
**Tech:** React 18, TypeScript (strict), Vite, TanStack Query v5, Zustand, React Hook Form, Zod, Tailwind CSS

---

## Pages

| Route | Page | Description |
|---|---|---|
| `/login` | LoginPage | JWT auth |
| `/users` | UsersPage | User list + create/edit/deactivate |
| `/roles` | RolesPage | Role list + permission assignment |
| `/config` | SystemConfigPage | Key/value system settings |
| `/audit-logs` | AuditLogPage | Searchable/filterable audit history |

---

## Getting Started

```bash
npm install
npm run dev
# http://localhost:5175
```

### Prerequisites
- `auth-service` running on port 5001
- `admin-service` running on port 5003

---

## Environment Variables

`.env.local`:
```env
VITE_API_BASE_URL=http://localhost:5001
```

---

## Available Scripts

| Script | Description |
|---|---|
| `npm run dev` | Start dev server |
| `npm run build` | Production build to `dist/` |
| `npm test` | Vitest unit tests |
| `npm run test:coverage` | Tests with coverage |
| `npm run lint` | ESLint |
| `npm run type-check` | TypeScript check |

---

## Testing

```bash
npm test
```

Unit tests cover form validation schemas, permission toggle logic, and config key/value editing.

---

## Production Build

```bash
npm run build
# dist/ deployed to S3 + CloudFront via Terraform
```
