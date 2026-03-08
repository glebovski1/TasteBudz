# AGENTS.md

## Purpose

This file defines how AI agents must work in this repository.

It is not the primary source of product, architecture, domain, or API truth unless a repository-process rule is stated here.
Its purpose is to:

- direct agents to the correct source documents
- define document authority and fallback precedence
- define required workflow before and after changes
- prevent silent inconsistency, scope drift, and architectural drift

Use project documents for truth. Use this file for process.

---

## Project Stage

This project is currently in a design-to-implementation transition for a capstone-level MVP.

Agents should prioritize:

- consistency with approved documents
- maintainable implementation
- correct domain behavior
- controlled scope

Agents must not expand scope or redesign architecture unless explicitly instructed.

---

## Current Technical Baseline

- Backend stack: ASP.NET Core Web API on .NET 9
- Target architecture: single-deployable modular monolith
- Persistence target: SQL Server / Azure SQL
- Current implementation status: the backend implementation is still early, and the backend docs should be treated as the target shape unless the user approves a change in direction

Implication:

- when authoritative docs and current code disagree, treat the docs as the target state unless the user explicitly instructs otherwise
- do not weaken approved docs just to fit incomplete or placeholder code
- if recent code appears to invalidate a document, report the mismatch explicitly before changing either side

---

## Authoritative Documents

Agents must consult the relevant project documents before making non-trivial changes.

### Topic-specific primary authorities

- `docs/TasteBudz_Functional_Requirements.md`
  Source of truth for product scope, MVP boundaries, feature behavior, and acceptance expectations.

- `docs/backend/backend-decisions.md`
  Source of truth for accepted backend decisions, ADRs, and previously resolved backend policy choices.

- `docs/backend/backend-architecture.md`
  Source of truth for system structure, layering, module boundaries, and implementation shape.

- `docs/backend/domain-model.md`
  Source of truth for domain concepts, core relationships, aggregate boundaries, and business invariants.

- `docs/backend/api-endpoints.md`
  Source of truth for API surface and request/response contract expectations.

### Secondary and supporting documents

- concept, review, design, research, notes, and report documents

These may provide context, but they are lower authority than the primary documents above.

---

## Authority and Precedence

Prefer the most specific authoritative document for the topic at hand:

- product scope and MVP boundary questions -> `docs/TasteBudz_Functional_Requirements.md`
- accepted backend decisions and policy choices -> `docs/backend/backend-decisions.md`
- layering, module ownership, and structural questions -> `docs/backend/backend-architecture.md`
- entities, lifecycle rules, and invariants -> `docs/backend/domain-model.md`
- routes and contract shape -> `docs/backend/api-endpoints.md`

If the conflict crosses topics or the correct authority is unclear, use this fallback precedence order:

1. `docs/TasteBudz_Functional_Requirements.md`
2. `docs/backend/backend-decisions.md`
3. `docs/backend/backend-architecture.md`
4. `docs/backend/domain-model.md`
5. `docs/backend/api-endpoints.md`
6. supporting concept, design, and research documents

If the conflict is significant, ambiguous, or materially affects implementation, the agent must report it explicitly before changing code or docs.
Agents must not silently choose one interpretation when a meaningful contradiction exists.

---

## Required Agent Workflow

For any non-trivial task, the agent must follow this process.

### 1. Understand the task

Identify:

- goal
- affected modules
- affected domain concepts
- affected APIs
- affected documents

### 2. Read relevant source documents

Before implementing, review the relevant authoritative documents.

Examples:

- scope or feature behavior questions -> functional requirements
- accepted backend choice questions -> backend decisions
- layering or module questions -> architecture
- entity or invariant questions -> domain model
- endpoint or contract questions -> API document

### 3. Check for inconsistency

Determine whether the requested change conflicts with:

- MVP scope
- accepted backend decisions
- architecture rules
- domain invariants
- API expectations
- current documented behavior

If conflict exists, report it before proceeding.

### 4. Propose a bounded approach

For major tasks, the agent should define:

- what will change
- what will not change
- impacted files or modules
- impacted documents
- risks
- any simplifications

### 5. Implement within boundaries

Agents must implement changes in a way that respects the authoritative documents.

### 6. Update affected documentation

If implementation changes documented behavior, structure, contracts, or accepted decisions, the relevant documents must be updated.

### 7. Re-check consistency

After changes, the agent must verify that code and documents still align.

---

## Small vs Major Tasks

Agents should use judgment.

For small tasks such as typo fixes, renames, formatting-only edits, or tightly scoped refactors with no behavioral impact:

- use a lighter workflow
- read only the documents needed for the change
- do not perform ceremonial document review that adds no value

For major tasks such as new features, rule changes, endpoint work, persistence design, or cross-module changes:

- follow the full workflow above
- state the documents reviewed
- call out risks, contradictions, and scope boundaries explicitly

---

## Change Management Rules

Agents must follow these rules:

- Do not invent missing requirements silently.
- Do not expand MVP scope without explicit approval.
- Do not introduce architectural changes without explicit approval.
- Do not resolve document contradictions silently.
- Do not update code in a way that leaves core documents outdated.
- Do not treat supporting notes as higher authority than the primary documents.
- Do not expose persistence entities directly as API contracts.
- Do not let clients control server-owned lifecycle state.

If a requested change requires a new decision, the agent should surface it as a decision point rather than hiding it inside implementation.
If the decision is approved and it affects backend policy, architecture, or contract direction, record it in `docs/backend/backend-decisions.md`.

---

## Simplification Rules

If implementation of a requested feature becomes too complex for the current project stage, agents should prefer:

- simpler implementation
- narrower UX flow
- reduced technical complexity
- phased delivery

However, simplification must not silently violate:

- approved MVP scope
- core domain rules
- required moderation or safety behavior
- important architectural boundaries
- accepted backend decisions

When simplifying, the agent must clearly state:

- what is being simplified
- why
- what behavior is preserved
- what tradeoff is introduced

---

## Documentation Update Rules

When implementation changes behavior, structure, contracts, or accepted decisions, agents must update the relevant documentation.

Use this mapping:

- scope or feature behavior changes -> `docs/TasteBudz_Functional_Requirements.md`
- new backend policy or accepted technical direction -> `docs/backend/backend-decisions.md`
- structural or layering changes -> `docs/backend/backend-architecture.md`
- entity or invariant changes -> `docs/backend/domain-model.md`
- endpoint or contract changes -> `docs/backend/api-endpoints.md`

If no documentation update is required, the agent should explicitly state why.

---

## High-Risk Areas

The following areas are easy to break and should trigger extra document review before implementation:

- event participation, capacity, and lifecycle rules
- group ownership and membership rules
- messaging authorization and moderation interactions
- discovery and Budz matching rules
- privacy, blocking, and safety-related behavior

When touching these areas, re-check `backend-decisions.md`, `domain-model.md`, and any relevant requirements sections before finalizing changes.

---

## Expected Output for Major Tasks

Before major implementation work, the agent should provide:

### Goal

What is being changed and why.

### Documents Reviewed

Which authoritative documents were checked.

### Affected Areas

Modules, entities, endpoints, and documents involved.

### Risks or Conflicts

Any contradictions, uncertainties, or scope risks.

### Proposed Approach

A bounded implementation plan.

### Simplifications

Any deliberate reduction in complexity.

After major work, the agent should provide:

### Summary of Changes

What was changed.

### Files Updated

Code and documentation files modified.

### Consistency Check

Whether the result still aligns with requirements, decisions, architecture, domain model, and API documentation.

### Follow-Up Issues

Any unresolved concerns or decision points.

---

## Default Agent Behavior

Unless explicitly instructed otherwise, agents should assume:

- this is a capstone MVP, not a production-scale platform
- consistency is more important than feature expansion
- documented decisions are preferred over agent creativity
- small, clear, maintainable changes are preferred over broad redesign
- the simplest implementation that preserves documented invariants is usually the correct starting point

If ambiguity still affects correctness after reviewing the relevant documents, stop and raise the decision explicitly instead of inventing policy in code or docs.

