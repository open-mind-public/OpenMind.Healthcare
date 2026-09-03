# Phase 0 Research: Diet Analytics

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)

Every finding below was checked against the running codebase rather than assumed. Where a query
capability was in doubt, it was executed against a real SQLite database and the result recorded —
those are marked **probed**.

---

## R-001: Where this feature lives

**Decision**: Additive inside `DietApi`. A new `Features/DietAnalytics/` slice on
`/api/diet-analytics`, a new read-model repository, new domain services, and one new Angular page.
No new service, port, container, volume, database file or frontend prefix.

**Rationale**: The feature reads the diet context's own tables and nothing else. Constitution
Principle I is satisfied by staying inside the context that owns the data; the eight
new-service obligations in Architecture & Technology Constraints apply to *new services* and are
inherited unchanged from the host.

**Alternatives considered**: A separate reporting service. Rejected outright — it would need to
read `diet.db`, which Principle I forbids, or replicate it, which is a great deal of machinery for
a read that already has the data locally.

---

## R-002: No schema change, no migration

**Decision**: This feature adds no table, no column and no migration. Everything it reports is
derived on demand from data already stored.

**Rationale**: Constitution Principle VI governs schema changes and seeds; having neither is the
cheapest way to comply. It also makes FR-026 (analytics reflect a change to underlying data on the
next view) true by construction rather than by cache invalidation.

**Alternatives considered**: A stored, incrementally-maintained analytics table. Rejected: it
would need invalidating on every food entry write, and any bug in that invalidation shows up as
analytics that quietly disagree with the log they claim to describe. R-008 shows the read is fast
enough that there is nothing to buy with that risk.

---

## R-003 (probed): Entry-level aggregation is queryable

**Question**: `FoodEntry` is an owned collection on `LoggedDay` with no `DbSet` of its own. Can
per-entry figures be grouped in SQL at all, or must entries be loaded into memory?

**Probed result**: `context.LoggedDays.Where(...).SelectMany(d => d.Entries).GroupBy(...)`
translates to a single SQL statement with an `INNER JOIN` onto `FoodEntries`, and returns correct
figures. Verified for grouping by `MealType`, by `(FoodLibraryItemId, FoodName)`, and with
`Sum(e => e.Nutrition.Calories)` reaching into the owned `NutritionValues` columns.

**Decision**: All energy aggregation happens in SQL. Entries are never loaded into memory to be
totalled.

**Rationale**: It works, and it is the only approach that meets SC-004 at three years of history.
Calories are `int` columns precisely so this stays a numeric aggregation (ADR 0002).

---

## R-004 (probed): Food category needs a join, because entries do not snapshot it

**Question**: FR-008 wants intake grouped by food category. Does `FoodEntry` carry one?

**Probed result**: It does not. `FoodEntry` snapshots `FoodName`, `ServingLabel`, `Quantity`,
`MealType` and `Nutrition`, and keeps `FoodLibraryItemId` for provenance — but not `Category`.
Joining `FoodEntries` to `FoodLibraryItems` on `FoodLibraryItemId` translates cleanly and produced
correct per-category figures.

**Decision**: Category comes from a join to the food library at read time.

**Rationale**: It is a join within one bounded context between two tables the context owns, so no
principle is strained. And unlike name and nutrition — which are snapshotted deliberately so a
catalogue correction cannot rewrite history — category is a *classification*, not a figure the
member saw and acted on. Reclassifying a food should reclassify it everywhere, including in past
periods.

**Consequence worth stating**: this is the one figure in the feature that can change for a past
period without the member changing anything. It is the correct behaviour, and it is a difference
from the snapshotting rule that a reader will otherwise trip over.

**Alternatives considered**: Adding `Category` to `FoodEntry`. Rejected: it needs a migration and a
backfill, and it would freeze a classification that is better kept current.

---

## R-005 (probed): Macronutrients are summed in memory, never in SQL

**Question**: FR-010 needs protein, carbohydrate and fat totalled across a period. ADR 0002 says
`decimal` maps to SQLite `TEXT` and must not be aggregated in SQL. What actually happens?

**Probed result**: `Sum(d => d.Totals.ProteinG)` over two days holding 9.7 g each returned
**19.4 — the correct answer**. It did not throw and it did not warn.

**Decision**: Macronutrients are aggregated **in memory**, by reading each day's stored
`Totals` row for the period and summing them in the domain. Never in SQL.

**Rationale**: The probe result is the argument *for* the rule, not against it. A prohibited
operation that silently returns the right answer on small, clean data is worse than one that
throws: it will pass every test written against a handful of days and then drift on a value that
does not round-trip cleanly through text. ADR 0002 is an accepted decision of this codebase and
this feature will not be the one to quietly reinterpret it.

The cost of complying is nothing. Three years is at most ~1,100 day rows, each already carrying
its own stored totals, and summing 1,100 decimals in memory is immeasurable next to the query that
fetched them.

**Note on scope**: this is why macronutrients are reported at period level only. A per-meal or
per-food macronutrient split would need per-entry decimals, which is a different and much larger
read. The spec asks for neither.

---

## R-006 (probed): Time of day, and the timezone problem

**Question**: FR-014 wants intake distributed across the hours of the day. Every instant in this
application is stored UTC and no member timezone is stored anywhere. Whose hours?

**Probed result**: `GroupBy(e => e.LoggedAt.Hour)` translates and works, returning UTC hours.
`GroupBy(e => new { e.LoggedAt.Hour, Quarter = e.LoggedAt.Minute / 15 })` also translates and
works.

**Decision**: The client sends its UTC offset in minutes. The query returns a **96-bucket
quarter-hour histogram in UTC**, and the offset is applied by rotating those buckets.

**Rationale**: Rotating a 24-bucket hourly histogram is only correct for whole-hour offsets, and
several hundred million people live at +05:30 (India), +05:45 (Nepal) or +09:30 (parts of
Australia). Quarter-hour buckets make the rotation exact for every real-world offset, because all
of them are multiples of fifteen minutes. Ninety-six rows is not a cost worth optimising away.

**Alternatives considered**:

- *Group by UTC hour and label it UTC.* Honest but useless: a member in Singapore would see their
  breakfast at midnight.
- *Store a timezone on the member's profile.* A better long-term answer, but it belongs to the
  platform and `UserApi`, not to a diet feature, and Principle I forbids reaching for it here.
- *Shift the timestamp inside the query.* Would push date arithmetic into SQLite for no gain over
  rotating buckets after the fact.

**Not in doubt**: day-of-week (FR-013) comes from `LoggedDay.Date`, which is a `DateOnly` calendar
day with no timezone component at all. Only time-of-day has this problem.

---

## R-007 (probed, inconclusive): Ordering by a decimal column

**Question**: ADR 0002 warns that `ORDER BY` over a `decimal`-as-`TEXT` column sorts
lexicographically. Does it?

**Probed result**: **Inconclusive.** The probe's two rows held identical values (9.7 and 9.7), so
the returned order proves nothing either way.

**Decision**: Treat ADR 0002's prohibition as standing. Nothing in this feature orders or
range-compares a decimal column in SQL. The one ranking the feature does — top contributing foods,
FR-007 — orders by summed calories, which is `int`.

**Rationale**: A test that did not disprove something has not proved its opposite. Recording the
probe as inconclusive is the honest outcome; designing around the ADR costs nothing here.

---

## R-008: Meeting the two-second budget at three years

**Decision**: Each of the four reads is a small number of grouped queries over indexed columns,
and none of them loads an entry into memory.

| Read | Queries | Rows returned |
|--------|--------|--------|
| Intake (US1) | day-state counts; group by meal; group by food; group by category | ~4 + top-N foods + 8 categories |
| Macronutrients (US2) | one projection of per-day totals | one row per logged day (≤ ~1,100) |
| Patterns (US3) | per-day totals for weekday; quarter-hour histogram | ≤ ~1,100 and 96 |
| Observations (US4) | none of its own — it reads the three above | — |

**Rationale**: `LoggedDays` already carries an index on `(UserId, Date)`, added for the calendar in
001, and it is exactly the index these range reads need. `FoodEntries` is reached by its foreign
key to `LoggedDayId`, which EF Core indexes by default. The existing three-year scale test
(`ThreeYearHistoryTests`) established that this shape of read is comfortably inside budget.

**Consequence**: the analytics page issues four requests, one per section, rather than one large
one. Each returns in its own time and each section renders when it arrives.

---

## R-009: The read model sits beside the aggregate repositories, not inside them

**Decision**: A new `IDietAnalyticsRepository` returns flat aggregate rows — `(meal, kcal, count)`,
`(foodId, name, kcal, times)`, `(category, kcal)`, `(date, calories, protein, carbs, fat, target)`,
`(hour, quarter, kcal)`. It returns no aggregate roots and no entities. Domain services turn those
rows into the figures and observations the member sees.

**Rationale**: This is the one place the feature departs from the pattern the rest of the service
follows, and it is deliberate. `ILoggedDayRepository` exists to load and save the `LoggedDay`
aggregate; making it also answer "which foods contributed most calories in the last ninety days"
would give one interface two unrelated jobs and tempt someone into loading a thousand aggregates to
answer a question SQL can answer in one statement.

The split keeps Principle II intact where it matters: the repository does arithmetic the database is
good at (summing and grouping), and every judgement — what counts as a logged day, which target
applied when, what share is large enough to be worth remarking on — stays in the domain, where it is
testable without a database.

Recorded in the plan's Complexity Tracking, and proposed as ADR 0004.

**Alternatives considered**:

- *Extend `ILoggedDayRepository`.* Rejected as above.
- *Load aggregates and compute in memory.* Rejected: three years is ~4,400 entries across ~1,100
  aggregates, and it fails SC-004 for no benefit.
- *Raw SQL.* Rejected: EF Core translates all of this (R-003, R-004, R-006), so dropping to SQL
  strings would lose compile-time checking and gain nothing.

---

## R-010: Observations are rules, not statistics

**Decision**: Each observation is an `IObservationRule` — a small class declaring a family, a
minimum number of logged days, a threshold, and an `Evaluate(figures) → Observation?`. An
`ObservationEngine` domain service runs them all, discards those below their minimum or threshold,
keeps only the strongest per family, and orders the survivors by strength.

**Rationale**: This is the shape that makes FR-018, FR-020 and FR-022 testable rather than
aspirational. Each rule's minimum and threshold are declared data, so SC-008 can assert *for every
rule* that it stays silent below its minimum without knowing what the rule says. Strength is
computed from how far past its threshold a rule fired, so the ordering is a pure function of the
figures — which is FR-020, determinism, by construction rather than by discipline.

The family field is what implements FR-022: "a third of your intake is after 21:00" and "your
evening meal is 45% of your intake" are the same observation wearing two hats, and only the stronger
should appear.

**Alternatives considered**:

- *Free-form generated prose.* Rejected. FR-020 requires the same data to produce the same words
  every time, and FR-019 draws a line that only fixed, reviewable wording can be held to.
- *Statistical significance testing.* Rejected as false precision: a member's food log is not a
  sample from a population, and a p-value would dress a threshold up as something it is not.
- *Thresholds tuned per member.* Rejected for this release — unreviewable, and it makes SC-010
  impossible to run against a fixed list.

**The rules proposed for this release**, each with its family, minimum and threshold, are
enumerated in [data-model.md](./data-model.md). Every one is stated as a fact with its figure
attached, and none diagnoses, judges or instructs (FR-019).

---

## R-011: What "logged days" means, and why every average says so

**Decision**: A day counts as logged when a `LoggedDay` exists for it with at least one entry.
Every average states which denominator it used, and the two available denominators are *logged
days in the period* and *all days in the period*.

**Rationale**: This is the single easiest way for an analytics feature to lie. A member who logged
three days of a thirty-day month has an average over those three days that looks like a monthly
average and is not one. FR-003 and SC-003 exist for this, and the existing programme already
distinguishes "not logged" from "logged nothing" — a day emptied of its last entry is deleted
rather than kept at zero, so the distinction is already true in the data.

**Consequence**: intake averages use logged days; the day-state split (FR-009) uses all days,
because "how many days did I not log" is the question there. Both are labelled.

---

## R-012: Periods and the comparison window

**Decision**: Four presets — last 7 days, last 30, last 90, and the whole plan. Each is clamped to
`[plan.StartDate, today]`, and the response states the period actually analysed. The comparison
window is the immediately preceding span of the same length, clamped the same way.

**Rationale**: Matches the weekly comparison already built for exercise in 002, so the two features
answer "versus before" the same way. Clamping is FR-002; stating the narrowed period is what stops a
member who joined last week from thinking they had a quiet quarter.

**Alternatives considered**: An arbitrary date picker. Deferred by the spec's Assumptions; it is
additive later, since the period is resolved in one domain service and everything downstream takes
a resolved period.

**Edge case that falls out of this**: for the whole-plan preset there is no preceding window. The
comparison is reported as unavailable rather than as zeros, because zeros would read as "you did
nothing before", which is a claim about a period that does not exist.

---

## R-013: Charts without a charting library

**Decision**: Render every distribution with CSS grid, flexbox and inline SVG. No new frontend
dependency.

**Rationale**: The frontend's dependencies are Angular, RxJS, tslib and zone.js — nothing else. The
existing calendar draws a full year of day cells with CSS grid, so the precedent for "draw it
ourselves" is already set and already consistent with the design tokens. What this feature needs is
horizontal bars, a 24-hour histogram and a seven-column weekday chart; all three are a `div` with a
width, and all three then inherit theming, dark mode and the token palette for free.

**Alternatives considered**: A charting library. It would draw prettier charts and would arrive with
its own colour system, its own theming story and a bundle already 240 kB over budget. Revisit if a
later feature needs something genuinely beyond bars.

---

## R-014: The guarantee carried forward from 002

**Decision**: Analytics presents intake and, where relevant, exercise as separate figures. No
endpoint, DTO, domain type or screen in this feature produces a value combining the two.

**Rationale**: 002 established that recorded exercise never becomes calories available to eat
(FR-013 to FR-019 there, FR-023 here). An analytics feature is the most natural place in the whole
application for someone to helpfully add a "net calories" column, which is exactly why the
prohibition is restated as a requirement and gated by SC-007.

**How it is enforced**: the same way it was in 002 — a structural test asserting that no analytics
response type carries a field combining the two, so the guarantee is checked by the build rather
than by review. The exercise contract is not read by this feature at all in this release, because
the shared timeline that would need it is out of scope.

---

## Open questions carried into implementation

None blocking. Two things to confirm while building, neither of which changes the design:

1. That EF Core emits the expected index usage for the `(UserId, Date)` range plus join — confirm
   with a query plan at the three-year scale test rather than assuming.
2. The exact top-N for contributing foods (FR-007). Ten is proposed; it is a display constant, not
   a design decision, and the query is `Take(n)` either way.
