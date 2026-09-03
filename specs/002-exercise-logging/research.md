# Phase 0 Research: Exercise Logging

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Date**: 2026-09-03

Every unknown carried into planning is resolved here. R-001 to R-003 close assumptions the
specification deferred; the rest are construction decisions raised by reading the existing code.

---

## R-001: Exercise belongs inside the diet service

**Decision**: Exercise lives in `DietApi` — same project, same database, same migration history.
No new service, no new ports, no compose changes.

**Rationale**: The specification places it in the diet programme, and the data backs that up: an
estimate needs the member's current weight, which `DietPlan` already owns, and the calendar has to
show exercise beside eating. A separate service would mean either duplicating body weight or
reaching across a context boundary for it — both worse than the coupling they would avoid.

This is additive: nothing existing changes shape, so the port allocation table in
[001-diet-tracking research R-011](../001-diet-tracking/research.md) still stands.

**Alternatives considered**:

- *A separate `ExerciseApi` programme.* The right call if exercise later grows its own goals,
  plans and coaching. Today it has none of those, and splitting now would buy a boundary that only
  costs.

**Traceability**: spec Assumptions, FR-017.

---

## R-002: Exercise days are their own aggregate, not part of `LoggedDay`

**Decision**: `ExerciseDay` is a new aggregate root — one per `(DietPlanId, Date)` — owning that
date's `ExerciseEntry` collection, with its own concurrency token and stored daily totals. It sits
beside `LoggedDay`, not inside it.

**Rationale**: This looks like it should hang off `LoggedDay`, and it must not. `LoggedDay` is
created lazily by the first food entry and **deleted when its last entry is removed** — that is how
"not logged" stays distinct from "logged nothing". So a date where a member ran but ate nothing they
recorded has no `LoggedDay` row at all.

Owning exercise there would therefore mean:

- exercise on a food-free day would have nowhere to live, and
- deleting the last meal of a day would silently delete that day's run,

which is exactly what **FR-013** forbids. The lifecycles differ, so the aggregates differ.

The volume argument that drove [ADR 0001](../../OpenMind.Healthcare/adrs/0001-aggregate-boundaries-for-high-volume-child-records.md)
points the same way: exercise arrives once or twice a day, so three years is ~1,000 rows — well
inside "own it" territory for the entries within a day, and the per-day aggregate keeps writes
constant-time.

**Alternatives considered**:

- *Own `ExerciseEntry` under `LoggedDay`.* Fails FR-013 outright, per the lifecycle argument above.
- *Make `ExerciseEntry` its own aggregate root, one per session.* Removes the per-day total and the
  per-day conflict boundary FR-012 needs, pushing both into a handler and breaking Principle II.

**Traceability**: FR-004, FR-012, FR-013.

---

## R-003: Energy estimates use MET values from a published compendium

**Decision**: Each catalogue activity carries a **MET** value. The estimate is

```
kcal = MET × weightKg × durationHours
```

using the member's current weight from their plan, rounded to whole kilocalories.

**Rationale**: One MET is by definition roughly one kilocalorie per kilogram per hour, so this is
the standard closed form and needs only values the plan already has. MET values come from the
Compendium of Physical Activities, the reference table used across the field; the specific edition
is a content decision recorded with the seed.

Intensity lives in the catalogue rather than on the entry, which is how the compendium itself is
organised — "Running, 8 km/h" and "Running, 12 km/h" are separate rows with separate METs. That
choice removes a field from every entry and stops members self-rating something they rate badly.

Worked example: running at 8 km/h (8.3 MET), 70 kg, 45 minutes → `8.3 × 70 × 0.75 = 436 kcal`.

**Alternatives considered**:

- *Heart-rate or wearable-derived expenditure.* More accurate and explicitly out of scope; there is
  no device integration in this release.
- *A flat kcal-per-minute per activity.* Ignores body weight, so it would tell a 50 kg member and a
  110 kg member they burned the same — visibly wrong to anyone who compares.

**Traceability**: FR-014, FR-017, FR-025.

---

## R-004: The estimate is snapshotted, and stored as an integer

**Decision**: When an entry is recorded, the activity name and the computed kilocalorie estimate are
copied onto it. The entry keeps the activity's id for provenance but never recomputes from the
catalogue. Kilocalories are `int`; duration is `int` minutes. `ExerciseDay` persists its own
totals.

**Rationale**: Two existing decisions apply unchanged. **FR-009** needs the same protection the food
library already gives logged meals — correcting a MET value must not silently rewrite what a member
was shown weeks ago. And [ADR 0002](../../OpenMind.Healthcare/adrs/0002-decimal-columns-and-aggregation-on-sqlite.md)
requires that anything the database aggregates be an integer, because EF Core maps `decimal` to
SQLite `TEXT`, which cannot be summed numerically — and **FR-022** sums minutes and kilocalories
across days.

Storing per-day totals also lets the calendar and the summary read one small row per day rather than
every entry, which is what meets **SC-004**.

The snapshot has a second consequence worth stating: because the estimate is fixed at recording
time, a member who later records a new weight does not see past estimates move. That is correct —
they burned what they burned at the weight they were.

**Traceability**: FR-009, FR-017, FR-022, SC-004, SC-007.

---

## R-005: The calendar reads exercise from its own endpoint

**Decision**: A separate range endpoint returns exercise summaries per day. The existing food-log
range contract is unchanged; the calendar fetches both and merges them client-side.

**Rationale**: **FR-021** requires exercise to be marked "without displacing" the eating
indication, and **FR-013** requires the two records to stay separate. Extending the food-log
response with an exercise field would make the eating contract carry exercise concerns, and would
put the two back in one payload just after the domain deliberately kept them apart.

Two parallel requests cost nothing here — both read one small indexed row per day — and the client
already merges data for the calendar.

**Alternatives considered**:

- *Extend the food-log range response.* One request instead of two, and the reason it was rejected
  is not performance but coupling: a member with exercise and no food would then need the food-log
  endpoint to return rows for days that have no food, which is precisely the confusion FR-013 is
  written to prevent.

**Traceability**: FR-013, FR-021, SC-004.

---

## R-006: Conflicts are refused, using the pattern already in place

**Decision**: `ExerciseDay` carries a `Guid Version` concurrency token, reassigned on every
mutation and configured with `.IsConcurrencyToken()`. A stale write fails and the endpoint answers
**409**.

**Rationale**: **FR-012** states the requirement, and the diet feature already solved exactly this
for `LoggedDay` ([001 research R-015](../001-diet-tracking/research.md)). Reusing the pattern keeps
one concurrency story in the codebase rather than two, and the same reasoning applies: a per-day
aggregate with stored totals can otherwise persist one session's total over another's entries.

**Traceability**: FR-012.

---

## R-007: Activity catalogue shape, seed and search

**Decision**: `ActivityType` is seeded reference data carrying a name, a normalised search name, a
category and a MET value — deliberately parallel to `FoodLibraryItem`. Search is a case-insensitive
prefix-then-substring match on the normalised name, capped at 20 results.

First-release seed: roughly **60–80 activities** across walking, running, cycling, swimming, gym and
strength, sports, home and garden, and everyday activity — with intensity variants where the
compendium distinguishes them.

**Judging corpus**: **SC-003** is only falsifiable against a fixed list, so a corpus of about 25
everyday activities is checked in beside the seed and asserted against it, chosen independently of
the seed. The food library's corpus decision is the precedent.

**Rationale**: Fewer activities than foods are needed — people do a narrower range of things than
they eat — but the same trade applies: a curated catalogue buys trustworthy numbers at the cost of a
member being unable to log something missing, which **FR-027** requires the app to say plainly.

**Traceability**: FR-003, FR-025, FR-026, FR-027, SC-003, SC-010.

---

## R-008: Where this appears in the interface

**Decision**: Three surfaces, one new nav item.

| Surface | What it does |
|--------|--------|
| **Today** (existing diet dashboard) | An exercise section beneath the meals: what was recorded, the day's total, and an add control |
| **History** (existing calendar) | A distinct marking on days with exercise, alongside the eating state |
| **Activity** (new page, new nav item) | The weekly summary of User Story 4 |

The programme registry gains one entry — `{ path: 'activity', label: 'Activity', icon: 'zap' }` —
and the left rail picks it up with no other change. That is the registry working as intended.

**On FR-019**: the explanation that logged exercise does not move the target belongs where the two
appear together — on Today, beside the estimate, and on the plan settings screen beside the activity
level. A member who logs exercise consistently and sees their target never move will read that as a
defect unless the interface says why.

**Traceability**: FR-019, FR-020, FR-021, FR-022.

---

## R-009: Marking a day distinctly without a fourth day state

**Decision**: Exercise is an **independent flag** on a calendar day, not a new `DayState` member.
The day cell keeps its eating colour and gains a small separate indicator.

**Rationale**: `DayState` has exactly three members and describes eating only — the diet feature
already refused to let "outside the plan" become a fourth. Folding exercise in would produce a
combinatorial state set (on-target-with-exercise, over-target-with-exercise, …) and make **FR-021**
impossible to honour, since one marking cannot show two independent facts.

**Traceability**: FR-013, FR-021, spec US3 scenario 2.

---

## R-010: An estimate that rounds to nothing

**Decision**: Estimates are rounded to whole kilocalories with a floor of 1 kcal for any recorded
session. A saved session never displays "0 kcal".

**Rationale**: A five-minute gentle stretch at 2.3 MET for a 60 kg member computes to about 12 kcal,
so this is rare — but a one-minute entry can round to zero, and showing "0 kcal" beside something a
member actually did reads as a bug rather than as rounding. The spec calls this out as an edge case.

**Traceability**: spec edge case "An estimate that rounds to nothing".

---

## R-011: Test project and migration

**Decision**: Tests go in the existing `DietApi.Tests` project, already registered in the solution.
One new migration, `AddExerciseLogging`.

**Rationale**: Same bounded context, same service, so no new project and no new registration.
`QuitSmokingApi.Tests` was added to the solution during 001, so `dotnet test` on the solution
already runs everything.

**Traceability**: Constitution Principle V, VI.
