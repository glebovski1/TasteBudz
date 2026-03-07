# Backend Decisions Log

This file is the single place to record backend architecture, implementation, and policy decisions for TasteBudz.

## How to Use This File

- Add a new entry for each meaningful backend decision.
- Keep entries short and concrete.
- Prefer updating with a new decision entry instead of rewriting history.
- Include the date, status, decision, and rationale.

## Decision Template

```md
## [BD-###] Short Decision Title

- Date: YYYY-MM-DD
- Status: Proposed | Accepted | Superseded | Deprecated
- Owners: Name(s) or team

### Context
What problem, constraint, or requirement led to this decision?

### Decision
What was decided?

### Rationale
Why was this option chosen over the alternatives?

### Consequences
- Positive impact
- Tradeoff
- Follow-up work
```

---

## [BD-001] Use This File as the Backend Decision Log

- Date: 2026-03-07
- Status: Accepted
- Owners: Backend team

### Context
The project needs a consistent place to document backend decisions so architecture and implementation choices do not get lost across chat, code comments, or separate documents.

### Decision
Store backend decisions in `docs/backend/backend-decisions.md` using a simple ADR-style format.

### Rationale
Keeping one backend decision log makes it easier to track why choices were made, review past decisions, and onboard contributors without spreading decision history across multiple files.

### Consequences
- Backend decisions now have a single documented source of truth.
- Future contributors should append entries here instead of creating ad hoc notes.
- If a decision changes later, a new entry should supersede the earlier one.
