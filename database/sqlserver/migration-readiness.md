# Migration Readiness Checklist

Foundation decisions locked for future SQL work:
- IDs stay UUID-based across API contracts and in-memory models.
- Event participant uniqueness must be enforced on `(EventId, UserId)`.
- Bud connection uniqueness must be enforced on the normalized user pair.
- Group membership uniqueness must be enforced on `(GroupId, UserId)`.
- Block uniqueness must be enforced on `(BlockerUserId, BlockedUserId)`.
- Audit tables remain append-only when the moderation module lands.
- Capacity, decision timing, and lifecycle rules remain service-owned even after SQL integration.

Deferred until the concrete persistence mechanism is chosen:
- executable migrations
- ORM model configuration
- stored procedure decisions
- index tuning beyond the baseline invariants
