---
name: review-test-publish-azure
description: Review recent TasteBudz changes, run Release build/test/regression checks, verify deployment safety, and publish the MVC/API/SignalR host to Azure App Service. Use when the user asks to "make a code review of recent changes test all new features, make sure old features is not broken and publish on Azure", asks to review-test-publish, or asks for Azure publish only after new and existing features are validated.
---

# Review Test Publish Azure

## Overview

Coordinate the full TasteBudz release update path: review recent changes, test new and existing behavior, fix blocking release issues when they are in scope, then publish the existing Azure App Service app.

This skill orchestrates validation and deployment. For the actual App Service update command sequence, load and follow `.agents/skills/azure-app-service-deployment/SKILL.md`.

## Guardrails

- Read `AGENTS.md` before starting, then read the authoritative docs relevant to the touched areas.
- Always read `docs/deployment/azure-app-service.md` and `src/TasteBudz.Database/sqlserver/README.md` before publishing.
- Use `.agents/skills/azure-app-service-deployment/scripts/update-published-app.ps1` for normal code-only updates to the existing App Service.
- Do not deploy if the build fails, tests fail, high-confidence blocking review findings remain, production schema changes are unapplied, or secrets would be included in the package.
- Do not print SQL passwords, publish credentials, access tokens, or full connection strings in updates or final answers.
- Do not treat generated publish output, local TestResults, or local environment-specific appsettings as source artifacts.

## Workflow

### 1. Establish Review Scope

1. Check current branch and worktree status.
2. Identify the recent-change range. Prefer `git merge-base origin/master HEAD` followed by `git diff origin/master...HEAD`; if that is not available, use the most defensible recent commit range and state the assumption.
3. Inspect changed files and classify affected modules, domain concepts, endpoints, persistence scripts, tests, and docs.
4. If there are unrelated user changes in the worktree, preserve them and work around them.

### 2. Review Against Project Truth

Read only the authoritative docs needed for the changed areas, using `AGENTS.md` precedence:

- product scope: `docs/TasteBudz_Functional_Requirements.md`
- backend decisions: `docs/backend/backend-decisions.md`
- architecture/layering: `docs/backend/backend-architecture.md`
- domain behavior: `docs/backend/domain-model.md`
- API contracts: `docs/backend/api-endpoints.md`
- testing expectations: `docs/backend/testing-strategy.md`

Review changed production code, tests, SQL scripts, docs, and deployment files for correctness, regressions, contract drift, missing tests, secret leakage, and generated artifacts committed by mistake.

If review finds a release-blocking issue and the fix is clear and in scope, fix it before deployment and rerun validation. If the issue requires a product or architecture decision, stop before publishing and report the decision point.

### 3. Validate New And Existing Behavior

Run full Release validation from the repository root:

```powershell
dotnet restore TasteBudz.sln
dotnet build TasteBudz.sln -c Release --no-restore
dotnet test TasteBudz.sln -c Release --no-build
git diff --check
```

Add or run focused tests for new behavior when the recent changes introduce a feature or bug fix that existing tests do not cover. Do not publish while known new behavior is untested or failing.

Before publishing, run a package-safety check when appsettings or deployment files changed:

```powershell
$publishDir = Join-Path $env:TEMP "tastebudz-publish-check"
Remove-Item $publishDir -Recurse -Force -ErrorAction SilentlyContinue
dotnet publish .\src\TasteBudz.Web.Mvc\TasteBudz.Web.Mvc.csproj -c Release -o $publishDir --no-build
Get-ChildItem $publishDir -Filter "appsettings*.json" | Select-Object Name
Remove-Item $publishDir -Recurse -Force
```

Expected package appsettings output is `appsettings.json` only.

### 4. Check Deployment Safety

Determine whether recent changes include production SQL Server schema or seed changes under `src/TasteBudz.Database/sqlserver`.

- If no schema changes are present, use the normal update script.
- If schema changes are present, apply and verify SQL scripts manually before the app update. The update script does not apply schema changes.
- If schema state cannot be verified safely, stop before publishing and report the blocker.

### 5. Publish To Azure

Load `.agents/skills/azure-app-service-deployment/SKILL.md`, then run the normal update script:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .agents\skills\azure-app-service-deployment\scripts\update-published-app.ps1
```

The script performs Release restore, build, tests, publish, zip deploy with Kudu fallback, artifact cleanup, and smoke checks. Capture and report its restore/build/test/deploy/smoke status.

### 6. Verify Production

Confirm the production app responds after deployment. For the current default app:

```powershell
curl.exe -s -o NUL -w "homepage %{http_code}`n" https://tastebudz-prod-23df46c9.azurewebsites.net/
curl.exe -s -o NUL -w "restaurants %{http_code}`n" https://tastebudz-prod-23df46c9.azurewebsites.net/api/v1/restaurants
curl.exe -s -o NUL -w "signalr_negotiate %{http_code}`n" -X POST "https://tastebudz-prod-23df46c9.azurewebsites.net/hubs/chat/negotiate?negotiateVersion=1"
```

Expected smoke status:

- homepage returns `200`
- unauthenticated restaurants API returns `401`, not `404`
- unauthenticated SignalR negotiate returns `401`, not `404`

If smoke fails, inspect App Service logs and report the failure instead of claiming publish success.

## Final Response

Report:

- review scope and documents reviewed
- code review findings, including fixed release blockers
- validation commands and pass/fail counts
- database/schema deployment status
- Azure deployment result and production URL
- smoke-check results
- files changed by the agent
- unresolved follow-ups or decision points

Keep the final answer concise and never include secrets.
