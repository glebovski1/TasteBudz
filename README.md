# TasteBudz

TasteBudz is a social dining coordination platform that connects people based on cuisine preferences, location, and availability.

## Repository Structure

docs/ – project documentation  
src/ – backend source code  
tests/ – automated tests  

## Tech Stack

Backend:
- ASP.NET Core Web API
- EF Core
- SQLite for local MVP development
- SQL Server for Azure production

Frontend:
- ASP.NET Core MVC

## Local Manual Testing

Use the local SQLite single-host script before publishing to Azure:

```powershell
.\start-dev.ps1 -ResetDatabase
```

The script forces local development settings, ignores any Azure SQL environment variables in the shell, creates `.codex-temp\TasteBudz.local.sqlite`, applies the source-controlled SQLite schema, and seeds test users. All seeded users use password `TasteBudz123!`; for example, sign in as `alex`.

Open `https://localhost:7115` or `http://localhost:5019` to test MVC, API, and SignalR together. Use `-SkipBuild` for faster restarts after the first build.

## Project Documentation

See `/docs` for concept, design, and requirements documents.
