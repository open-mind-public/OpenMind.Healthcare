# ADR 0001: Aggregate boundaries for high-volume child records

**Status**: Accepted
**Date**: 2026-09-02
**Context feature**: [specs/001-diet-tracking](../../specs/001-diet-tracking/plan.md) (research R-004)

## Context

`QuitJourney` owns its entire `SmokedDay` history as an EF Core `OwnsMany` collection. That works
because a smoked day is a rare event: a member on a successful quit journey accumulates a handful
of rows over months.

The diet subdomain has the same shape on the surface — a plan with dated child records — but not
the same volume. A member logs three to six food entries **every day**. Three years of use is
roughly 5,000 rows. Mirroring the `QuitJourney` pattern would mean loading, change-tracking, and
re-saving that entire history to record one breakfast item, making every write scale with how long
the member has been using the application.

## Decision

`LoggedDay` is its own aggregate root, referencing `DietPlan` by id with no navigation property and
no foreign key across the boundary. `DietPlan` still owns `WeightReading` and
`UnlockedAchievement`, which are genuinely low-volume.

The split is safe because **no invariant spans two days**. Every rule is either per-day (a day's
totals derive from that day's entries) or per-plan. Streaks read across days but only compute a
projection — they enforce nothing, so they need no transactional boundary.

## Consequences

- Writes stay constant-time regardless of tenure. A three-year history draws a calendar in under a
  second because the range query reads one summary row per day and never touches entries.
- A day must carry a denormalised `UserId` so queries filter by owner without crossing an aggregate.
- Two aggregates can now be edited concurrently, which is what made ADR 0003 necessary.
- The application no longer has one uniform pattern for "plan owns its history". A reader must ask
  which volume regime a child record is in before copying either shape.

## Rule of thumb for the next subdomain

Own the children when they arrive at human pace — a relapse, a weigh-in, a milestone. Split them
out when they arrive at daily-habit pace. The question is not "is this conceptually part of the
aggregate" but "will loading all of it to write one of them still be reasonable in three years".

## Alternatives rejected

- **One aggregate owning everything.** Maximum consistency with the existing reference
  implementation. Rejected purely on growth; at `SmokedDay` volumes it would have been right.
- **`FoodEntry` as its own aggregate.** Pushes the per-day total invariant out of the domain and
  into a handler, which the constitution's Principle II forbids.
