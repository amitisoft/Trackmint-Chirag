# GitHub Push Guide

Use this flow to publish TrackMint safely.

## Before You Stage Files

Confirm these local-only files stay out of Git:

- `backend/PersonalFinanceTracker.Api/appsettings.Development.json`
- `frontend/.env`
- `frontend/.env.local`
- `frontend/node_modules/`
- `frontend/dist/`
- `backend/**/bin/`
- `backend/**/obj/`
- `.dotnet/`

Check:

```powershell
git status --short
git check-ignore -v backend\PersonalFinanceTracker.Api\appsettings.Development.json
```

## Recommended Branch Setup

Rename the default branch to `main`:

```powershell
git branch -M main
```

## Stage the Repository

Stage everything that is meant to be versioned:

```powershell
git add .
```

Verify what will be committed:

```powershell
git status
```

## First Commit

Create a clean initial commit:

```powershell
git commit -m "Initial TrackMint application setup"
```

## Create the GitHub Repository

Create a new empty GitHub repository from the browser:

- Repository name: `trackmint`
- Visibility: private if hackathon rules allow, otherwise public
- Do not add a README, `.gitignore`, or license from GitHub since the local repo already has them

## Connect Local Repo to GitHub

Replace `<YOUR_GITHUB_REPO_URL>` with the URL from GitHub:

```powershell
git remote add origin <YOUR_GITHUB_REPO_URL>
git remote -v
```

Example:

```powershell
git remote add origin https://github.com/your-username/trackmint.git
```

## Push

Push the `main` branch:

```powershell
git push -u origin main
```

## After Push

Open the GitHub repo and quickly verify:

- source folders are present
- `README.md` renders correctly
- no real DB password is visible
- `appsettings.Example.json` exists
- `.env.example` exists
- container files are present for QA and hosting work

## Useful Follow-Up Commands

See changed files before later commits:

```powershell
git status
```

Commit later changes:

```powershell
git add .
git commit -m "Refine dashboard and app UI"
git push
```
