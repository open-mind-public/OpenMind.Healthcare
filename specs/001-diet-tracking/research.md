# Phase 0 Research: Diet Tracking

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Date**: 2026-09-02

Every unknown carried into planning is resolved here. Items R-001 to R-003 close assumptions the
specification deliberately deferred. Items R-004 to R-013 are construction decisions raised by
reading the existing code.

---

## R-001: Formula behind the suggested daily calorie target

**Decision**: Mifflin-St Jeor resting metabolic rate, multiplied by an activity factor, then
adjusted by goal type.

- RMR (male) = `10 × weightKg + 6.25 × heightCm − 5 × age + 5`
- RMR (female) = `10 × weightKg + 6.25 × heightCm − 5 × age − 161`
- Activity factors: sedentary 1.2, lightly active 1.375, moderately active 1.55, very active
  1.725, extra active 1.9
- Goal adjustment: lose −500 kcal/day, maintain 0, gain +400 kcal/day, consistency 0

**Rationale**: Mifflin-St Jeor is the formula the Academy of Nutrition and Dietetics recommends
for resting-energy estimation in non-clinical settings, and it outperforms Harris-Benedict on
modern populations. It needs only the four inputs the spec already collects (weight, height, age,
sex), so it introduces no new data requirement. The −500 kcal/day deficit corresponds to roughly
0.45 kg (1 lb) of weight loss per week, which is inside the commonly cited safe range.

**Alternatives considered**:

- *Harris-Benedict (revised)*: the older standard, known to overestimate by 5% or so in
  overweight populations. No advantage here.
- *Katch-McArdle*: more accurate, but requires body-fat percentage, which the spec does not
  collect and which members rarely know.
- *Fixed lookup table by sex and goal*: trivial to build, but produces the same number for a
  50 kg and a 110 kg member, which would undermine SC-002 (70% suggestion acceptance).

**Traceability**: FR-005, FR-017.

---

## R-002: Safe minimum daily calorie floor

**Decision**: A curated constant pair — 1,200 kcal/day for female members, 1,500 kcal/day for
male members — held as a named domain constant, not derived per member.

**Rationale**: These are the figures most widely published as the floor below which a diet is
unlikely to meet micronutrient needs without supervision. Holding them as one named constant
keeps the value reviewable in a single place, which matters because it is the one number in this
feature with a genuine duty of care attached. FR-007 clamps the *suggestion* to this floor; FR-008
allows the member to go below it deliberately after a warning, because blocking a member's own
choice outright would be paternalistic for a non-clinical wellbeing app.

**Alternatives considered**:

- *Derive a floor from RMR (e.g. never below RMR × 0.8)*: more personalised, but produces floors
  under 1,000 kcal for small members, which is exactly the outcome the floor exists to prevent.
- *Hard block below the floor*: rejected. The app is not a clinical tool and has no authority to
  refuse a member's stated target; a warning discharges the duty of care.

**Open for review**: These numbers should be confirmed by whoever owns clinical content before
release. Flagged in the spec's Assumptions and repeated here.

**Traceability**: FR-007, FR-008, SC-010.

---

## R-003: Macronutrient split for the suggestion

**Decision**: Goal-dependent percentage splits of the calorie target, converted to grams at
4 kcal/g protein, 4 kcal/g carbohydrate, 9 kcal/g fat.

| Goal | Protein | Carbohydrate | Fat |
|--------|--------|--------|--------|
| Lose weight | 30% | 40% | 30% |
| Maintain | 20% | 50% | 30% |
| Gain weight | 25% | 50% | 25% |
| Eat consistently | 20% | 50% | 30% |

**Rationale**: All four splits sit inside the Acceptable Macronutrient Distribution Ranges
(protein 10-35%, carbohydrate 45-65%, fat 20-35%), with the weight-loss split raising protein to
the top of the range, which is the standard adjustment for preserving lean mass in a deficit.
Percentages rather than fixed grams means the split stays coherent when a member overrides the
calorie target.

**Alternatives considered**:

- *Grams per kilogram of body weight for protein, remainder split*: closer to sports-nutrition
  practice, but harder to explain to a member and produces odd results at the extremes of weight.
- *No macro suggestion at all*: the spec allows members to leave macro targets unset, but
  offering nothing would waste data already collected.

**Traceability**: FR-005.

---

## R-004: Aggregate boundaries — a day is its own aggregate

**Decision**: Three member-owned aggregates rather than one.

- `DietPlan` — owns `WeightReading` and `UnlockedAchievement` collections
- `LoggedDay` — owns its `FoodEntry` collection, references the plan by id
- Reference data — `FoodLibraryItem`, `DietAchievement`, `EatingTip`

**Rationale**: This deviates from the `QuitJourney` precedent, which owns its entire `SmokedDay`
history, and the deviation is deliberate. A smoked day is a rare event — a member on a successful
quit journey has a handful. A food entry happens three to six times *every* day: three years of
logging is roughly 5,000 rows. Loading, change-tracking, and re-saving 5,000 owned rows to add one
breakfast item would put SC-006 (under 1 second) out of reach and would make every write scale
with the member's tenure.

The split is safe because no invariant in the specification spans two days. Every rule is either
per-day (FR-031, FR-032, FR-033 — totals derive from *that day's* entries) or per-plan (FR-001 to
FR-011). Streaks read across days (FR-035) but only compute a projection; they enforce nothing, so
they need no transactional boundary. The consistency boundary therefore lands exactly where the
invariants are.

`WeightReading` stays owned by `DietPlan` because it is capped at one row per date (FR-012) and is
member-paced — a few hundred rows at most — so the loading argument does not apply, and owning it
keeps the one-per-date invariant inside the aggregate where Principle II wants it.

**Alternatives considered**:

- *One `DietPlan` aggregate owning everything*: maximum consistency with the existing reference
  implementation, rejected on the growth argument above.
- *`FoodEntry` as its own aggregate*: would push the per-day total invariant (FR-033) out of the
  domain and into a handler, violating Principle II.

**Recorded in**: plan.md Complexity Tracking.

**Traceability**: FR-033, SC-006.

---

## R-005: Nutrition values are snapshotted onto the entry

**Decision**: When a food entry is created, the calories and macronutrient grams computed from the
library item are copied onto the entry. The entry keeps the library item's id for provenance, but
never recomputes its contribution from the library afterwards.

**Rationale**: FR-025 requires that correcting a library food does not retroactively change days a
member already saw assessed. A snapshot is the only way to guarantee that: any design that
recomputes from the live library would silently rewrite history the moment a typo in the seed data
is fixed. It also makes a `LoggedDay` self-contained — it can compute its own totals with no join
to the library, which is what lets the day aggregate stand alone under R-004.

**Alternatives considered**:

- *Recompute from the library on read*: smaller storage, but breaks FR-025 outright.
- *Version the library items and reference a version*: correct, and considerably more machinery
  than a feature with a curated, rarely-corrected catalogue needs.

**Traceability**: FR-024, FR-025, SC-009.

---

## R-006: The day's target is snapshotted too

**Decision**: `LoggedDay` stores the calorie and macro targets in force at the moment the day was
first created, and assesses itself against that snapshot rather than against the plan's current
target.

**Rationale**: FR-004 requires already-assessed days to keep the target that applied when they were
logged, and the edge case "target changed mid-history" requires that lowering a target must not
flip past days from on-target to over-target. Snapshotting is the same mechanism as R-005 and makes
day assessment a pure function of the day's own state — directly satisfying FR-033 and making the
aggregate testable with no plan present.

**Alternatives considered**:

- *Effective-dated target history on the plan*: a list of (from-date, targets) rows queried when
  assessing a day. More faithful to "what was the target on 3 March" for days never logged, but it
  makes assessment depend on two aggregates and buys nothing the snapshot does not.

**Traceability**: FR-004, FR-031, FR-033, SC-009.

---

## R-007: Earned achievements are persisted, not computed

**Decision**: Unlocking writes an `UnlockedAchievement` row (achievement id + earned date) onto the
`DietPlan`. Locked state and remaining progress are computed; earned state is stored.

**Rationale**: This differs from `AchievementStatusService` in the smoking area, which derives
achievement status entirely from current journey state. A derived design cannot satisfy FR-039
("MUST NOT revoke an achievement already earned"): a member who deletes a mis-logged entry would
watch a badge disappear. Storing the earned date is the only way the guarantee holds, and it also
gives FR-038 the "date earned" it requires, which a pure computation cannot supply.

**Alternatives considered**:

- *Mirror the smoking area exactly and compute*: consistent, but fails FR-039 and User Story 5
  scenario 4.

**Traceability**: FR-038, FR-039.

---

## R-008: Day states and what deleting the last entry does

**Decision**: Three states, resolved as follows.

- **Not logged** — the day has no entries. Does not break a streak; does not count toward days
  logged; excluded from average intake.
- **On target** — the day has at least one entry and total calories are at or below the day's
  target snapshot.
- **Over target** — the day has at least one entry and total calories exceed it.

Deleting a day's final entry returns the day to **not logged**, and the day row itself is removed.

**Rationale**: FR-032 already fixes the streak-relevant half — a day with no entries is not logged
rather than compliant. The residual question was what deletion does, and returning to "not logged"
is the only answer consistent with it; leaving a zero-calorie shell behind would create a day that
counts as a perfect on-target day precisely because the member logged nothing, which is the exact
outcome FR-032 exists to prevent. Removing the row also keeps "days logged" honest.

A missed day therefore *interrupts* a streak without *breaking* it in the punitive sense: the
current streak counts back from today through consecutive on-target days and stops at the first
day that is not on target, whether that day was over target or never logged. This is stated
explicitly because it is the single most misread rule in habit trackers.

**Traceability**: FR-032, FR-035, edge cases "A day with no entries" and "Deleting the last entry".

---

## R-009: Food library shape and search

**Decision**: `FoodLibraryItem` carries a name, a searchable normalised name, a category, and an
owned collection of `ServingSize` rows, each with a label ("100 g", "1 medium", "1 cup"), a gram
weight, and the nutrition values for that serving. Search is a case-insensitive `LIKE` prefix and
substring match on the normalised name, capped at 20 results, ordered by whether the match is a
prefix and then alphabetically.

First-release seed: approximately 150 to 200 common foods across staples, proteins, dairy, fruit,
vegetables, prepared meals, snacks, and drinks.

**Rationale**: Nutrition values belong on the serving, not the food, because "1 medium banana" and
"100 g banana" are different numbers and the member picks one of them (FR-019, FR-024). A `LIKE`
scan is sufficient at 200 rows and needs no full-text extension in SQLite; SC-004's 1-second bar is
met comfortably. Seeding is idempotent via the existing `if (!context.X.Any())` guard, satisfying
FR-021 and SC-011.

The 150-200 figure is what it takes to give SC-004's "85% of common foods found" a fighting
chance. If seeding proves thin during implementation, the honest response is to widen the seed, not
to relax SC-004.

**Alternatives considered**:

- *SQLite FTS5*: warranted at thousands of rows; unnecessary machinery at 200.
- *External nutrition API*: rejected at spec time (Q1 = curated library) and separately
  incompatible with the constitution's self-contained, per-service SQLite model.

**Traceability**: FR-019, FR-020, FR-021, FR-022, SC-004, SC-011.

---

## R-010: Calories are stored as integers, and each day stores its own totals

**Decision**: Calories are `int` (whole kcal) everywhere. Macronutrient grams are
`decimal(6,1)`. `LoggedDay` persists its own `TotalCalories` and macro totals, recomputed by the
aggregate on every entry change.

**Rationale**: This is a SQLite constraint, not a style preference. EF Core maps `decimal` to
SQLite `TEXT`, and aggregate or ordering operations over a TEXT column do not behave numerically —
`Sum` and `Average` over a decimal column are documented as unsupported or lossy on SQLite. FR-035
needs an average of daily intake across days, so the column being averaged must be numeric:
`int` kcal is exact, and nutrition labels are whole kcal anyway, so nothing is lost.

Macro grams stay decimal because they are only ever computed and displayed within a single day —
in memory, inside the aggregate — and never aggregated in SQL.

Persisting each day's totals is what makes the calendar and statistics views read one small row per
day instead of every entry, which is how SC-006 is met over a three-year history. It introduces a
denormalisation invariant — stored total must equal the sum of entries — which is safe precisely
because R-004 keeps the entries and the total inside the same aggregate, recomputed together on
every mutation. A domain test asserts the invariant directly.

**Alternatives considered**:

- *`double` for calories*: avoids the TEXT problem but introduces float drift in a number members
  compare against a target for equality.
- *Compute totals on read from entries*: no denormalisation to maintain, but every calendar render
  would load every entry in the period, and the three-year case fails SC-006.

**Traceability**: FR-031, FR-033, FR-035, SC-006, SC-008.

---

## R-011: Port, volume, and proxy allocation

**Decision**:

| Resource | Value | Already taken by |
|--------|--------|--------|
| Dev port (http) | 3005 | 3003 QuitSmokingApi, 3004 UserApi *and* QuitSmokingApi's https profile |
| Container port | 5000 | same as every service |
| Docker host port | 5436 | 5431 api, 5434 user-api, 5435 ui |
| Named volume | `diet-sqlite-data` | `sqlite-data`, `user-sqlite-data` |
| Database file | `/app/data/diet.db` | `quitSmoking.db`, `users.db` |
| Frontend path prefix | `/diet-api` → rewritten to `/api` | `/api`, `/user-api` |
| Compose service name | `diet-api` | `api`, `user-api`, `ui` |

The constitution requires this table to exist before implementation begins.

**Defect found while allocating**: `QuitSmokingApi`'s `https` launch profile binds
`https://localhost:3004`, which is `UserApi`'s http port. Running both under the https profile
collides. The new service will define an http profile only, matching `UserApi`, and will not
inherit the pattern. Fixing the existing profile is out of scope for this feature and should be
raised separately.

**Traceability**: Constitution, Architecture & Technology Constraints, items 7-8.

---

## R-012: Units

**Decision**: Store metric — kilograms and centimetres. Display unit (kg/lb, cm or ft-in) is a
client-side preference held in the browser; the API accepts and returns metric only.

**Rationale**: Keeping conversion at the edge means one canonical stored value, no round-trip
drift, and no migration if the preference model later moves server-side. The spec's assumption
already commits to display units not affecting stored values.

**Traceability**: FR-012, FR-014, spec Assumptions.

---

## R-013: Health endpoint

**Decision**: Map `GET /health` returning 200 with a minimal body, outside authorization.

**Rationale**: Every existing `Dockerfile` declares `HEALTHCHECK CMD curl -f
http://localhost:5000/health`, but no service actually maps `/health` — the check can only ever
fail, and `curl` is additionally not present in the `aspnet:10.0` runtime image. The new service
maps the endpoint so its own health check is meaningful. Whether to add `curl` to the image or
switch to a `dotnet`-based probe is settled during implementation; the endpoint is the part this
feature owes. The same gap in the two existing services is pre-existing and out of scope.

**Traceability**: Constitution, Architecture & Technology Constraints, item 6.

---

## R-014: Test project registration

**Decision**: Add `DietApi.Tests` to `OpenMind.Healthcare.sln`, and add the already-existing
`QuitSmokingApi.Tests` at the same time.

**Rationale**: The constitution carries `TODO(TEST_PROJECT_IN_SOLUTION)` recording that
`QuitSmokingApi.Tests` exists on disk but is absent from the solution, which makes Principle V
unenforceable from a solution-wide build — `dotnet test` on the solution runs nothing. Registering
the new test project is mandatory for this feature; sweeping up the existing one is a one-line
addition in the same file and clears the constitution's only outstanding TODO.

**Traceability**: Constitution Principle V and its follow-up TODO.
