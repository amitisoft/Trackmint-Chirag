# TrackMint Backend Guide

## Stack

- ASP.NET Core Web API
- EF Core with PostgreSQL
- JWT auth
- modular monolith layering:
  - `Domain`
  - `Application`
  - `Infrastructure`
  - `Api`

## Projects

- `PersonalFinanceTracker.Domain`: TrackMint entities and enums
- `PersonalFinanceTracker.Application`: DTOs, contracts, exceptions
- `PersonalFinanceTracker.Infrastructure`: EF Core, services, auth helpers, background worker
- `PersonalFinanceTracker.Api`: controllers, middleware, startup

For local execution, the verified build path is the `PersonalFinanceTracker.Api` project. It compiles the layered source tree directly, so use that project as the entrypoint when running locally or in containers.

## Important Notes

- Database initialization currently uses `EnsureCreated()` for hackathon speed.
- Default categories are seeded on user registration.
- Refresh tokens and password reset tokens are stored in PostgreSQL.
- The recurring worker runs every 5 minutes.

## Local Run

From `backend/`:

```powershell
$env:DOTNET_CLI_HOME='c:\Users\Lenovo\Desktop\Hackathon\.dotnet'
dotnet restore .\PersonalFinanceTracker.Api\PersonalFinanceTracker.Api.csproj
dotnet run --project .\PersonalFinanceTracker.Api\PersonalFinanceTracker.Api.csproj
```

## Swagger

When the API is running locally, Swagger UI is available at:

- `http://localhost:5151/swagger`
- `https://localhost:7293/swagger`

To test secured endpoints, click `Authorize` and provide:

```text
Bearer YOUR_ACCESS_TOKEN
```

## Config Files

- `PersonalFinanceTracker.Api/appsettings.json`: safe shared defaults for repo and container-oriented local setup
- `PersonalFinanceTracker.Api/appsettings.Development.json`: local machine overrides, ignored by Git
- `PersonalFinanceTracker.Api/appsettings.Example.json`: example you can copy from when onboarding a new machine

Example connection-string shape:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=personal_finance_tracker;Username=postgres;Password=your-password"
  }
}
```

For GitHub, keep real local passwords only in `appsettings.Development.json`.

## Main API Surface

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/forgot-password`
- `POST /api/auth/reset-password`
- `GET /api/dashboard/summary`
- `GET/POST/PUT/DELETE /api/transactions`
- `GET/POST/PUT /api/accounts`
- `POST /api/accounts/transfer`
- `GET/POST/PUT/DELETE /api/budgets`
- `GET/POST/PUT /api/goals`
- `POST /api/goals/{id}/contribute`
- `POST /api/goals/{id}/withdraw`
- `GET/POST/PUT/DELETE /api/recurring`
- `GET /api/reports/category-spend`
- `GET /api/reports/income-vs-expense`
- `GET /api/reports/account-balance-trend`
- `GET /api/reports/export/csv`
