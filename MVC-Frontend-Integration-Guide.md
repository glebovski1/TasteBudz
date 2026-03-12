# How MVC Works in TasteBudz

Last verified: 2026-03-11

This guide explains how ASP.NET Core MVC works in this repository today.

Important distinction:

- `src/TasteBudz.Backend` is a controller-based Web API backend
- `src/TasteBudz.Web.Mvc` is the actual MVC frontend with controllers, Razor views, and view models

The backend remains the system of record. The MVC app is a server-rendered UI that calls the backend over HTTP.

## Documents Reviewed

- `docs/backend/backend-architecture.md`
- `docs/backend/backend-decisions.md`
- `docs/backend/frontend-api-guide.md`
- `docs/backend/implementation-status.md`

## Big Picture

The MVC app follows this shape:

```text
Browser
-> ASP.NET Core MVC host
-> MVC controller
-> backend API client
-> TasteBudz.Backend /api/v1/*
-> backend DTO
-> MVC view model
-> Razor view
-> HTML response
```

Repository-specific rule:

- business rules stay in `TasteBudz.Backend`
- MVC controllers orchestrate UI flows
- Razor views render HTML
- MVC view models shape data for the page
- backend API clients handle the HTTP boundary

That matches the documented architecture: frontend-agnostic backend API, thin HTTP/UI layer, and server-owned business rules.

## Where The MVC App Lives

The MVC frontend is `src/TasteBudz.Web.Mvc`.

Current implemented MVC surface:

- account creation
- login
- logout
- profile edit / onboarding completion
- profile dashboard view
- shared error page

Current MVC controllers:

- `Controllers/AccountController.cs`
- `Controllers/ProfileController.cs`
- `Controllers/AppController.cs`

Current MVC views:

- `Views/Account/CreateAccount.cshtml`
- `Views/Account/Login.cshtml`
- `Views/Profile/Edit.cshtml`
- `Views/Profile/View.cshtml`
- `Views/App/Error.cshtml`

## Startup And Routing

`src/TasteBudz.Web.Mvc/Program.cs` sets up the MVC host.

What it does:

1. binds `BackendApi:BaseUrl` from configuration
2. registers `AddControllersWithViews()`
3. enables ASP.NET session state
4. enables cookie authentication for the MVC site
5. registers MVC frontend services and backend API clients
6. configures the pipeline and endpoint mapping in this order:
   - exception handler / HSTS outside development
   - HTTPS redirection
   - static assets
   - routing
   - session
   - authentication
   - authorization
7. maps the default route to `Account/Login`

Practical result:

- anonymous users land on the login page first
- authenticated users use the MVC cookie for page access
- backend bearer tokens stay inside server-side MVC session state

## The Main Pieces

### 1. Controllers

Controllers handle browser requests and decide which backend calls to make.

Examples:

- `AccountController` handles login, register, logout, and post-auth redirect logic
- `ProfileController` loads dashboard/profile data and posts profile edits
- `AppController` serves the shared error page

These controllers are intentionally thin. They do not implement TasteBudz business policy like event capacity, moderation, or discovery rules.

### 2. Backend API Clients

`Services/Backend/` contains small clients for backend feature areas:

- `AuthApiClient`
- `OnboardingApiClient`
- `ProfileApiClient`
- `PreferenceApiClient`
- `PrivacyApiClient`
- `DashboardApiClient`

These are the MVC app's integration boundary with the backend. Controllers call these clients instead of building raw `HttpRequestMessage` objects directly.

### 3. Request Executor

`Services/Backend/BackendApiRequestExecutor.cs` is the shared authenticated HTTP path for protected backend calls.

It is responsible for:

- reading the current backend access token from the MVC session service
- attaching `Authorization: Bearer ...`
- sending the HTTP request to the backend
- retrying once after token refresh when the backend returns `401`
- signing the user out locally if refresh fails

This keeps token plumbing out of individual controllers and area-specific API clients.

### 4. Session And Authentication

The MVC app uses two layers of auth state:

1. ASP.NET Core cookie authentication
2. server-side session storage containing the backend session snapshot

`Services/Session/UserSessionService.cs` is the bridge between them.

It:

- stores backend access token, refresh token, expiry, and current-user data in ASP.NET session
- creates a cookie-authenticated `ClaimsPrincipal` for the MVC site
- clears both the session snapshot and cookie during logout or token expiry

This means:

- the browser authenticates to the MVC app with a cookie
- the MVC app authenticates to the backend with bearer tokens

## Contracts vs View Models

The MVC app keeps backend contract shapes separate from page shapes.

Backend contract DTOs live in:

- `Services/Backend/Contracts/`

These mirror the backend API payloads used by the MVC frontend.

Page-facing models live in:

- `ViewModels/`

Examples:

- `LoginViewModel`
- `RegisterViewModel`
- `ProfileEditViewModel`
- `DashboardViewModel`

Why this separation matters:

- backend contracts match the HTTP API
- view models match what a Razor page needs
- the MVC app can reshape backend data without leaking backend DTOs directly into the views

Example:

- `DashboardApiClient` fetches a `DashboardDto`
- `DashboardViewModel.FromDto(...)` maps it into cards the Razor view renders

## How Requests Flow Here

### Login Flow

`/Account/Login` works like this:

1. browser requests the login page
2. MVC returns `Views/Account/Login.cshtml`
3. user submits the form
4. `AccountController.Login` model-binds into `LoginViewModel`
5. `AuthApiClient` calls `POST /api/v1/auth/login`
6. `UserSessionService.SignInAsync(...)` stores backend tokens and signs in the MVC cookie principal
7. MVC calls `GET /api/v1/onboarding/status`
8. user is redirected to:
   - `/Profile/Edit` if onboarding is incomplete
   - `/Profile/View` if onboarding is complete

### Dashboard Flow

`/Profile/View` works like this:

1. cookie auth allows the request into the controller
2. `ProfileController.View` calls the backend onboarding endpoint
3. if onboarding is incomplete, MVC redirects to `/Profile/Edit`
4. otherwise MVC calls `GET /api/v1/me/dashboard`
5. the backend response is mapped into `DashboardViewModel`
6. `Views/Profile/View.cshtml` renders the page

### Profile Edit Flow

`/Profile/Edit` is the clearest example of MVC orchestration in this repo.

GET request:

1. MVC fetches onboarding status
2. MVC fetches profile data
3. MVC fetches preferences
4. MVC fetches privacy settings
5. MVC combines those backend responses into one `ProfileEditViewModel`
6. Razor renders one page backed by multiple backend endpoints

POST request:

1. Razor form posts into `ProfileEditViewModel`
2. MVC runs view-model validation and normalization
3. MVC sends:
   - `PATCH /api/v1/profiles/me`
   - `PUT /api/v1/preferences/me`
   - `PATCH /api/v1/privacy-settings/me`
4. MVC redirects back to `/Profile/View`

Important boundary:

- MVC performs UI validation for usability
- backend validation remains authoritative

## What The Razor Views Do

The views are strongly typed and presentation-focused.

Patterns used here:

- layouts via `Views/Shared/_Layout.cshtml`
- Tag Helpers through `Views/_ViewImports.cshtml`
- antiforgery protection on POST forms
- validation summaries and field-level validation
- page-specific view models instead of persistence models

The views also contain repo-specific explanations that reflect backend ownership. For example, the profile edit page explicitly tells the user that onboarding completeness and preference matching are backend-driven.

## Folder Map

The useful non-generated MVC structure is:

```text
src/TasteBudz.Web.Mvc/
  Controllers/
  Options/
  Services/
    Backend/
    Session/
  ViewModels/
  Views/
  wwwroot/
  Program.cs
```

How to read that structure:

- `Controllers/`: browser request handling
- `Options/`: typed configuration such as backend base URL
- `Services/Backend/`: HTTP integration with `TasteBudz.Backend`
- `Services/Session/`: local auth/session bridge
- `ViewModels/`: page-specific models
- `Views/`: Razor markup
- `wwwroot/`: CSS, JS, static assets

Note:

- there is a `Models/` folder in the MVC project, but the current implementation primarily uses `ViewModels/` plus backend contract DTOs instead

## Testing Strategy For MVC

MVC integration tests live in `tests/TasteBudz.Web.Mvc.IntegrationTests`.

They do not depend on a live backend server.

Instead, `TasteBudzMvcFactory` replaces the named backend `HttpClient` handlers with `StubBackendApiHandler`, so tests can verify:

- redirects
- authentication behavior
- rendered HTML
- antiforgery-protected form posts
- JSON bodies sent from MVC to the backend API

This is a good fit for the current architecture because it tests MVC behavior without duplicating backend integration coverage.

## How To Add A New MVC Feature

Follow this pattern:

1. confirm the backend endpoint already exists and matches the docs
2. add or extend a backend API client in `Services/Backend/`
3. add or extend backend contract DTOs only as needed
4. add a page-specific view model in `ViewModels/`
5. add controller actions
6. add Razor views
7. add MVC integration tests with the stub backend handler

Do not put product rules in the MVC app just because the UI needs them. If a rule affects correctness, it belongs in `TasteBudz.Backend`.

## Current Scope Boundary

As of 2026-03-11, the MVC app is intentionally narrower than the backend API surface.

Implemented in MVC now:

- auth screens
- onboarding/profile editing
- dashboard/profile summary

Not yet surfaced as MVC pages:

- events UI
- groups UI
- discovery UI
- notifications UI
- moderation UI
- chat UI

That is not an architectural problem. It simply means the MVC frontend is currently a partial UI over a broader backend.

## Summary

MVC in this repo is a separate server-rendered frontend that sits on top of the backend API.

Use this mental model:

- controllers coordinate page flows
- backend API clients talk to `TasteBudz.Backend`
- session service bridges MVC cookie auth and backend bearer tokens
- view models shape data for Razor
- backend remains the owner of business rules and lifecycle state
