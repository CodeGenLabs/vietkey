# ADR-0001 - Adopt an anchored claim store

- Date: 2026-11-04
- Status: Accepted

## Context

Knowledge about this system was spread across prose documents that nothing
checked. Prose cannot be verified, so it drifts silently and is then either
trusted when it is wrong or ignored when it is right.

## Decision

System knowledge lives in anchored claims under `docs/system/`, each pointing
at the code it describes, each checked by `forge check`. Staleness is detected
deterministically against those anchors; no model decides whether a claim is
still true.

## Alternatives considered

- **Keep free prose and review it periodically.** Rejected: review debt is
  invisible, so the review never happens on the documents that need it.
- **Let a model check the documentation against the code.** Rejected on
  measurement: models detect documentation faults well when the prose changed
  and badly when only the implementation did, which is the case that matters.

## Consequences

Every change must account for the claims its diff touches, which is new work at
the point of change and is the entire point. Nothing here is enforced until the
first claims exist - run `forge bootstrap`, or write them as you learn them.
