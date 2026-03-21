# TrackMint

Full-stack hackathon implementation of TrackMint built with:

- `React + TypeScript`
- `ASP.NET Core 10 Web API`
- `PostgreSQL`
- container-ready files for `Podman`

## Repository Layout

```text
.
|- backend/
|  |- PersonalFinanceTracker.Api
|  |- PersonalFinanceTracker.Application
|  |- PersonalFinanceTracker.Domain
|  `- PersonalFinanceTracker.Infrastructure
|- frontend/
|- docs/
`- compose.yaml
```

## Core Features

- JWT auth with refresh token flow
- accounts and wallet management
- transaction CRUD with filtering and transfer support
- monthly budgets and utilization tracking
- savings goals with contribution and withdrawal flows
- recurring transactions with a background worker
- dashboard analytics and reporting charts
- CSV export
- responsive UI for desktop and mobile

## Local Development Order

1. Finish WSL + Podman setup.
2. Create a PostgreSQL database named `personal_finance_tracker`.
3. Copy `backend/PersonalFinanceTracker.Api/appsettings.Example.json` into your local `appsettings.Development.json` values if your DB credentials differ.
4. Restore and run backend.
5. Install frontend dependencies and run the React app.
6. Register a test user and start seeding live data from the UI.

## Swagger API Docs

After starting the backend locally, Swagger is available at:

- `http://localhost:5151/swagger`
- `https://localhost:7293/swagger`

For protected endpoints, use Swagger's `Authorize` button and enter:

```text
Bearer YOUR_ACCESS_TOKEN
```

## GitHub Safety

- `backend/PersonalFinanceTracker.Api/appsettings.Development.json` is for your local machine only and is ignored by Git.
- `frontend/.env.example` is committed as the frontend environment template.
- `backend/PersonalFinanceTracker.Api/appsettings.Example.json` is committed as the backend environment template.
- Do not commit real database passwords, JWT production keys, Azure secrets, or personal `.env` files.

Detailed setup:

- [Backend README](./backend/README.md)
- [Frontend README](./frontend/README.md)
- [Setup Notes](./docs/SETUP.md)
- [GitHub Push Guide](./docs/GITHUB_PUSH.md)
- [Deployment Notes](./docs/DEPLOYMENT.md)

## Status

Azure deployment is intentionally left pending until you share the company deployment doc. The repo is structured so we can add Azure-specific container and service wiring without rewriting the app.
