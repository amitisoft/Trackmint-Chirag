# TrackMint

TrackMint is a full-stack personal finance tracker built with:

- `React + TypeScript`
- `ASP.NET Core 10 Web API`
- `PostgreSQL`
- `Podman`-ready container configuration

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

## Features

- JWT auth with refresh token flow
- accounts and wallet management
- transaction CRUD with filtering and transfer support
- monthly budgets and utilization tracking
- savings goals with contribution and withdrawal flows
- recurring transactions with a background worker
- dashboard analytics and reporting charts
- CSV export
- responsive UI for desktop and mobile

## Local Development

1. Create a PostgreSQL database named `personal_finance_tracker`.
2. Configure backend settings using `backend/PersonalFinanceTracker.Api/appsettings.Example.json`.
3. Restore and run the backend.
4. Install dependencies and run the frontend.
5. Register a user and start using the application.

## Swagger API Docs

After starting the backend locally, Swagger is available at:

- `http://localhost:5151/swagger`
- `https://localhost:7293/swagger`

For protected endpoints, use Swagger's `Authorize` button and enter:

```text
Bearer YOUR_ACCESS_TOKEN
```

## Configuration Notes

- `backend/PersonalFinanceTracker.Api/appsettings.Development.json` is for your local machine only and is ignored by Git.
- `frontend/.env.example` is committed as the frontend environment template.
- `backend/PersonalFinanceTracker.Api/appsettings.Example.json` is committed as the backend environment template.
- Do not commit real database passwords, JWT production keys, Azure secrets, or personal `.env` files.

## Documentation

- [Backend README](./backend/README.md)
- [Frontend README](./frontend/README.md)
- [Setup Notes](./docs/SETUP.md)
