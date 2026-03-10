# ASP.NET MVC Frontend Integration Guide

## Goal

Add an ASP.NET MVC frontend to TasteBudz without breaking the documented backend direction:

- backend remains the system of record
- business rules stay in the backend API
- MVC acts as the UI layer, not a second business layer

## Current Repo Context

- Current backend project: `src/TasteBudz.Backend`
- Current backend style: ASP.NET Core Web API on `net9.0`
- Documented architecture: frontend-agnostic API, single-deployable modular monolith backend

## Options

| Option | What it means | Pros | Cons | Fit |
|---|---|---|---|---|
| Separate MVC project | Add `src/TasteBudz.Web.Mvc` and let it call the backend API | Clean separation, matches current docs, easiest to evolve later | Two apps to run locally | Recommended |
| MVC + API in one host | Add MVC controllers/views into `TasteBudz.Backend` | Fastest to prototype, one deployable app | Blurs API and UI responsibilities, harder to keep boundaries clean | Acceptable only for capstone speed |
| MVC BFF project | MVC app also acts as backend-for-frontend and hides tokens/server calls | Best control over auth/session UX | More moving parts and more code than MVP needs | Later, if auth complexity grows |

## Recommended Approach

Use a **separate MVC frontend project**.

Why this is the best fit for this repo:

- it preserves the documented "frontend-agnostic HTTP API" backend direction
- it keeps controllers/views focused on presentation
- it avoids moving business logic into Razor views or MVC controllers
- it lets the backend remain reusable for future mobile or SPA clients

## Suggested Structure

```text
src/
  TasteBudz.Backend/
  TasteBudz.Web.Mvc/
tests/
  TasteBudz.Backend.UnitTests/
  TasteBudz.Backend.IntegrationTests/
```

## Short Setup Guide

### 1. Create the MVC project

```powershell
dotnet new mvc -n TasteBudz.Web.Mvc -o src/TasteBudz.Web.Mvc
dotnet sln add src/TasteBudz.Web.Mvc/TasteBudz.Web.Mvc.csproj
```

### 2. Keep responsibilities clear

- `TasteBudz.Backend`: APIs, auth rules, domain workflows, SignalR hubs
- `TasteBudz.Web.Mvc`: controllers, views, view models, frontend composition
- Do not duplicate event, group, discovery, or moderation rules in MVC

### 3. Let MVC call the backend through typed clients

Register typed `HttpClient` services in the MVC app for areas like:

- Auth
- Profiles
- Restaurants
- Events
- Groups
- Discovery

This keeps backend API usage centralized and avoids controller-to-controller coupling.

### 4. Keep API contracts as the source of truth

- Use the backend `/api/v1/...` endpoints as the integration boundary
- Map API DTOs into MVC-specific view models
- Do not bind MVC views directly to persistence entities

### 5. Handle auth at the frontend edge

For MVC, the practical MVP pattern is:

- user signs in through the MVC app
- MVC calls backend auth endpoints
- MVC stores session state in secure server-side cookie/session handling
- MVC attaches backend access tokens when calling protected API endpoints

If that feels too heavy for the first milestone, start with anonymous/read-only pages and add authenticated flows next.

### 6. Use backend SignalR for chat

The backend docs already position chat as backend-owned real-time behavior.
For event chat and group chat, the MVC frontend should connect to backend SignalR hubs rather than reimplementing chat in the MVC host.

## If You Want The Fastest Capstone Path

If speed matters more than separation, you can place MVC inside `TasteBudz.Backend`:

- add `builder.Services.AddControllersWithViews();`
- keep API routes under `/api/v1`
- add MVC routes for pages
- keep backend services as the only location for business rules

This is faster, but it should be treated as a delivery shortcut, not a change in architecture policy.

## Recommendation Summary

Start with:

1. `TasteBudz.Backend` as the backend API and SignalR host
2. `TasteBudz.Web.Mvc` as a separate server-rendered frontend
3. typed `HttpClient` integration from MVC to backend
4. API DTO to view model mapping in the MVC layer

That gives the cleanest path for the current repo and stays aligned with the existing architecture documents.
