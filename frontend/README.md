# Frontend Guide

## Stack

- React 19
- TypeScript
- Vite
- TanStack Query
- Zustand
- React Hook Form + Zod
- Recharts
- Axios

## Structure

```text
src/
|- components/
|- features/
|- services/
|- store/
|- styles/
`- types/
```

## Local Run

From `frontend/`:

```powershell
npm install
npm run dev
```

If your backend runs on a different URL, create an environment variable before running:

```powershell
$env:VITE_API_BASE_URL='http://localhost:5151/api'
```

For GitHub, keep only `frontend/.env.example` in the repo. Do not commit a real `.env` file with environment-specific values.

## Major UI Areas

- auth screens
- dashboard
- transactions
- budgets
- goals
- reports
- recurring transactions
- accounts
- settings and category management

## Notes

- Auth state is stored with Zustand persistence.
- Axios automatically attaches JWT access tokens.
- A 401 response triggers refresh-token retry logic.
- CSV export uses the current bearer token.
