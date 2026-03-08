# Backend Development Approach

This document describes the default backend implementation approach for TasteBudz.

## Core Rule

- Work on one primary module at a time.
- Work on one workflow at a time inside that module.
- Keep business logic in services.
- Keep controllers thin.
- Keep repositories focused on persistence only.

## Development Cycle

For each module:

1. Define module scope and boundaries.
2. List the workflows inside the module.
3. Define use cases, rules, invariants, edge cases, and done criteria.
4. Define DTOs, service methods, and repository methods.
5. Write unit tests for the current workflow.
6. Implement service logic.
7. Implement repository support.
8. Add thin controller endpoints.
9. Add integration tests.
10. Add concurrency and security tests when needed.
11. Fix issues until the workflow is stable.
12. Repeat for the next workflow in the same module.

## Completion States

- `Backend-logic ready`: service logic and API behavior are implemented and tested.
- `Backend-complete`: real persistence and required concurrency-sensitive behavior are also proven.

## Practical Notes

- Start with foundation work before feature modules.
- Do not spread work across many half-built modules.
- High-risk modules such as Events should usually reach `Backend-complete` before moving too far ahead.
- Track current implementation progress in `docs/backend/implementation-status.md`.
