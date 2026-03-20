# Setup Notes

## Machine Prerequisites

- Windows 11
- WSL2 enabled
- Ubuntu distro installed in WSL
- Podman installed and machine initialized
- Node.js installed
- .NET SDK installed
- PostgreSQL installed and running
- Azure CLI installed

## Database

Create the local database:

```sql
CREATE DATABASE personal_finance_tracker;
```

If your PostgreSQL password is not `postgres`, update:

- `backend/PersonalFinanceTracker.Api/appsettings.Development.json`

Use `backend/PersonalFinanceTracker.Api/appsettings.Example.json` as the template for local overrides.

## Backend Start

```powershell
cd backend
$env:DOTNET_CLI_HOME='c:\Users\Lenovo\Desktop\Hackathon\.dotnet'
dotnet restore .\PersonalFinanceTracker.Api\PersonalFinanceTracker.Api.csproj
dotnet run --project .\PersonalFinanceTracker.Api\PersonalFinanceTracker.Api.csproj
```

## Frontend Start

```powershell
cd frontend
npm install
$env:VITE_API_BASE_URL='http://localhost:5151/api'
npm run dev
```

## Podman Readiness

Before container work:

```powershell
wsl -l -v
podman machine list
podman info
podman run hello-world
```
