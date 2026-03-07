---
name: "doc-consistency-reconciler"
description: "Compare two project documents, detect contradictions, omissions, terminology drift, scope drift, unsupported requirements, and invalid assumptions, then run a human-in-the-loop reconciliation workflow before applying minimal approved edits. Use when reconciling requirements, architecture, testing strategy, API specs, backlog docs, policies, or similar project documentation that must stay aligned."
---

# Doc Consistency Reconciler

## Overview

Compare two related project documents, surface real inconsistencies, and reconcile them through a controlled human-in-the-loop workflow. Prefer issue-by-issue review, minimal edits, and explicit re-validation over bulk rewrites.

## When to use

- Reconcile requirements vs architecture, testing, API, backlog, policy, or implementation docs.
- Detect contradictions, omissions, terminology drift, scope drift, unsupported requirements, invalid assumptions, role mismatches, or test gaps.
- Update one or both docs only after the user approves the resolution approach.

## Inputs

Expect:
- left document name
- right document name
- accessible content for both documents
- optional authority hints by topic

Treat the first document as `left` and the second as `right` for the entire session.

If authority hints are provided, apply them narrowly. Example:
- product behavior: requirements win
- technical decomposition: architecture wins
- validation approach: test strategy wins

If no authority hint exists, do not invent one.

## Workflow

1. Compare before editing.
   - Summarize the purpose of each document in 1-2 lines.
   - Assess whether the pair is mostly aligned, partially drifted, or materially inconsistent.
   - Build a numbered issue register before proposing edits.
2. Register only real issues.
   - Distinguish contradictions from valid specialization or harmless wording differences.
   - Prefer `needs human interpretation` when evidence is insufficient.
3. Classify each issue.
   - Include `Type`: `contradiction` | `omission` | `ambiguity` | `terminology drift` | `scope drift` | `unsupported requirement` | `invalid assumption` | `test gap`
   - Include `Severity`: `critical` | `major` | `minor`
   - Include `Confidence`: `high` | `medium` | `low`
   - Include recommended direction: `left-to-right` | `right-to-left` | `merge` | `create-new-rule` | `defer`
4. Process one issue at a time.
   - Explain `left` vs `right` precisely.
   - State why the difference matters and what breaks if it remains unresolved.
   - Provide multiple resolution options, not just one answer.
   - Recommend one option and wait for the user decision.
5. Plan edits before applying them.
   - Restate the approved resolution.
   - List the exact sections to update in each document.
   - Ask for approval before editing if patch authority has not already been granted.
6. Apply minimal targeted edits.
   - Preserve headings, numbering, IDs, tone, and document intent.
   - Update only the necessary text.
   - Do not rewrite whole sections unless the conflict cannot be resolved locally.
7. Re-check after every applied issue.
   - Confirm the specific issue is closed.
   - Detect side effects or newly created inconsistencies.
   - Keep a running decision ledger such as `DECISION-01`.
8. Finish with a full reconciliation pass.
   - List resolved issues, remaining unresolved issues, changes by document, residual risks, and a final verdict.
   - Explicitly state whether inconsistencies remain.

## Heuristics

Treat as likely real inconsistency when:
- one doc requires something the other forbids
- one doc assumes a feature or role the other omits or rejects
- lifecycle or state models do not align
- roles, permissions, ownership, or moderation rules differ
- test coverage does not validate a required behavior
- architecture cannot support a stated requirement
- terminology drift would likely cause implementation confusion
- one doc silently narrows or expands scope

Treat as likely acceptable difference when:
- one doc is intentionally higher-level
- added detail does not change operational meaning
- wording differs but behavior is equivalent
- the documents describe different phases and say so explicitly

## Required output templates

### Phase 1: Initial comparison

#### Comparison Summary
- Left doc purpose:
- Right doc purpose:
- Overall consistency status:
- Estimated number of issues:
- Recommended review order:

#### Issue Register
For each issue:
- ID: `ISSUE-01`
- Title:
- Type:
- Severity:
- Confidence:
- Left document position:
- Right document position:
- Summary:

Do not draft edits in Phase 1 beyond high-level direction.

### Phase 2: Interactive resolution

## `ISSUE-01` - Short title

### Conflict
- Left:
- Right:

### Why this is a problem
[Explain the mismatch.]

### Impact
[Explain the practical consequence.]

### Resolution options
1. `Left -> Right`
2. `Right -> Left`
3. `Merge / Refine`
4. `Keep both with clarified boundaries`
5. `Mark one doc authoritative for this topic`
6. `Defer / needs human interpretation`

For each option, include a short description plus pros and cons.

### Recommendation
[Choose the best current option and explain why.]

### Awaiting user decision
Do not move to edits until the user chooses or instructs otherwise.

### Phase 3: Approved change plan

## Approved resolution for `ISSUE-01`

### Selected approach
[Chosen option.]

### Planned document changes
- Left doc:
- Right doc:

### Draft edit summary
[Describe the exact targeted changes.]

Wait for explicit approval if editing authority has not already been granted.

### Phase 4: Apply edits

## Applied changes for `ISSUE-01`

### Left doc changes
[Before/after summary or patch-style explanation.]

### Right doc changes
[Before/after summary or patch-style explanation.]

### Verification
- Is the issue resolved?
- Any side effects introduced?
- Any follow-up issues created?

Then continue to the next issue.

### Phase 5: Final report

## Final reconciliation report

### Decisions
[List `DECISION-xx` entries.]

### Issues resolved
[List issue IDs.]

### Changes made
[List changes by document.]

### Remaining unresolved issues
[List or `none`.]

### Final consistency verdict
[`ready` | `mostly ready` | `not ready`]

### Residual risks
[List.]

### Recommended next step
[Proceed to implementation, revise tests, revisit API spec, etc.]

## Guardrails

- Be conservative; not every difference is a conflict.
- Do not invent requirements or silently choose a winner.
- Preserve intentional abstraction differences.
- Prefer reconciliation over false contradiction when one doc is higher-level and the other is detailed.
- Stop and mark `needs human interpretation` when evidence is insufficient.
- Maintain traceability from finding to decision to edit.
- Never silently edit without approval.