# ADR 0003: Earned state is stored, not derived

**Status**: Accepted
**Date**: 2026-09-02
**Context feature**: [specs/001-diet-tracking](../../specs/001-diet-tracking/plan.md) (research R-007, R-015)

## Context

`AchievementStatusService` in the smoking-cessation area derives achievement status entirely from
the member's current journey state. Nothing is stored; status is recomputed on every read.

That design has a failure mode nobody notices until it happens to a real member: **it can take an
achievement away**. A member who corrects a mis-logged entry, and whose streak consequently drops
below a threshold, watches a badge they earned weeks ago disappear. A derived design also has no
way to answer "when did I earn this", because the moment was never recorded.

## Decision

Where a member *earns* something, the earning is a fact to be recorded, not a conclusion to be
recomputed:

- Unlocking writes an `UnlockedAchievement` row carrying the achievement id and the date earned.
- Persisted state always wins. If the record exists, the achievement is unlocked regardless of what
  current statistics say.
- Unlocking is idempotent: awarding the same achievement twice is a no-op that preserves the
  original date.

The related decision, made for the same reason, is that **conflicting writes are refused rather than
merged**. `LoggedDay` carries a `Guid` concurrency token reassigned on every mutation; a write built
on a stale copy fails with 409 and the member reloads. Merging two edit sets would silently
resurrect an entry the member had deleted on another device.

## Consequences

- Achievements survive data corrections, and each carries a real earned date.
- The diet and smoking areas now handle achievements differently. That inconsistency is deliberate
  and this ADR is the record of why; the smoking area's derived design has the same latent defect
  and would benefit from the same change.
- Progress toward a locked achievement is still computed, so only the transition needs storage.
- Concurrency detection needs the client to echo a version on every write, which is visible in the
  API contract.

## Rule of thumb for the next subdomain

Anything a member would describe as "I earned that" or "I did that" is an event to record. Anything
they would describe as "how am I doing" is a projection to compute. If losing it would feel like the
application taking something away, store it.

## Alternatives rejected

- **Mirroring the smoking area's derived design.** Consistent, and fails the never-revoke
  requirement outright.
- **Last-write-wins on concurrent edits.** What the code does by default, and the specific outcome
  the specification forbids.
