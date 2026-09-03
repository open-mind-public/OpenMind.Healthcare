# Phase 1 Data Model: Exercise Logging

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Research**: [research.md](./research.md)

All types live in `DietApi.Domain`, alongside the existing diet model. Aggregates derive from
`AggregateRoot`, value objects from `ValueObject`. Every mapped entity gets
`entity.Ignore(e => e.DomainEvents)`.

## Aggregate map

```text
ExerciseDay (aggregate root)                 one per (DietPlanId, Date)
├── ExerciseTotals               owned VO      persisted totals, recomputed on every change
├── Version                      concurrency token, reassigned on every mutation
└── ExerciseEntry[]              owned entity  many per day

ActivityType (aggregate root)                seeded reference data
```

`ExerciseDay` references `DietPlan` by `DietPlanId` — a `Guid` column, no navigation property, no
foreign key across the aggregate boundary. It has **no relationship to `LoggedDay` at all**: the two
are independent per-day aggregates that happen to share a date, and neither can create or destroy
the other.

> **The load-bearing constraint.** `LoggedDay` is created by the first food entry and deleted when
> the last one is removed. If exercise lived inside it, a member deleting their dinner would delete
> that day's run, and exercise on a food-free day would have nowhere to live. FR-013 forbids both.

---

## ExerciseDay

**Root of**: exercise entries. **Identity**: `Id` (Guid), unique index on `(DietPlanId, Date)`.

Created lazily and destroyed when emptied, mirroring `LoggedDay`: the first entry for a date creates
the day, and removing the last one deletes it, so a date never carries an exercise day with zero
sessions.

| Field | Type | Notes |
|--------|--------|--------|
| `DietPlanId` | `Guid` | No navigation property. |
| `UserId` | `Guid` | Denormalised so every query filters by owner without a join (FR-030). |
| `Date` | `DateOnly` | The calendar day. Immutable once set (FR-007). |
| `Totals` | `ExerciseTotals` | Owned. Recomputed on every entry change (R-004). |
| `Version` | `Guid` | Concurrency token, reassigned on every mutation. A stale write raises `DbUpdateConcurrencyException`, which the endpoint turns into 409 (FR-012). |
| `Entries` | `IReadOnlyCollection<ExerciseEntry>` | Owned, private backing field. |
| `CreatedAt` / `UpdatedAt` | `DateTime` | From `AggregateRoot`. |

**Methods**

| Method | Behaviour |
|--------|--------|
| `static StartDay(dietPlanId, userId, date, planStartDate, asOf)` | Checks date rules, creates an empty day. Internal to the first `AddEntry`. |
| `AddEntry(activityTypeId, activityName, met, durationMinutes, memberWeightKg, asOf)` | Checks duration rules, computes and snapshots the estimate, appends, recomputes `Totals`. Emits `ExerciseLoggedEvent`. |
| `UpdateEntry(entryId, activityTypeId, activityName, met, durationMinutes, memberWeightKg)` | Same rules; re-estimates and re-snapshots, recomputes `Totals`. |
| `RemoveEntry(entryId)` | Recomputes `Totals`. Returns `bool`. `IsEmpty` then tells the repository to delete the day. |
| `IsEmpty` | `Entries.Count == 0`. |
| `EntriesInOrder()` | Ordered by when they were recorded, for display. |

**Rules** (`Domain/Rules/ExerciseEntryRules.cs`)

| Rule | Broken when | Requirement |
|--------|--------|--------|
| `ExerciseDateCannotBeInFutureRule` | `date > DateOnly.FromDateTime(asOf)` | FR-005 |
| `ExerciseDateCannotPrecedePlanStartRule` | `date < plan.StartDate` | FR-005 |
| `DurationMustBePositiveRule` | `durationMinutes <= 0` | FR-006 |
| `DurationWithinCeilingRule` | `durationMinutes > 1440` (a day) | FR-006 |

**Invariant asserted by test**: after any `AddEntry`, `UpdateEntry` or `RemoveEntry`,
`Totals.Minutes == Entries.Sum(e => e.DurationMinutes)` and
`Totals.Kilocalories == Entries.Sum(e => e.EstimatedKcal)` (R-004).

---

## ExerciseEntry (owned by ExerciseDay)

| Field | Type | Notes |
|--------|--------|--------|
| `Id` | `Guid` | `ValueGeneratedNever()`. |
| `ExerciseDayId` | `Guid` | Owner FK. |
| `ActivityTypeId` | `Guid` | Provenance only — never read back to recompute. |
| `ActivityName` | `string(120)` | Snapshotted, so a renamed activity does not rewrite history. |
| `Met` | `decimal(4,1)` | Snapshotted. Displayed and used for re-estimation on edit; never SQL-aggregated. |
| `DurationMinutes` | `int` | Whole minutes (FR-002). |
| `EstimatedKcal` | `int` | Snapshotted at recording time (R-004). Integer so the database can sum it. |
| `RecordedAt` | `DateTime` | UTC. |

---

## ActivityType (seeded reference data)

| Field | Type | Notes |
|--------|--------|--------|
| `Id` | `Guid` | |
| `Name` | `string(120)` | Display name, e.g. "Running, 8 km/h". |
| `SearchName` | `string(120)` | Lowercased, accent-stripped. Indexed; search matches on this. |
| `Category` | `ActivityCategory` | Stored as string. |
| `Met` | `decimal(4,1)` | From the compendium. Intensity lives here, not on the entry (R-003). |

**Seed**: 60–80 activities across the eight categories, guarded by
`if (!context.ActivityTypes.Any())` (FR-025, SC-010).

---

## Value objects

### ExerciseTotals

`Minutes` `int`, `Kilocalories` `int`. Static `Zero()` and `Create(...)`; `Plus(ExerciseTotals)`.
Both integers so the weekly summary can aggregate them in SQL — see ADR 0002.

### ActivityCategory

Enum: `Walking`, `Running`, `Cycling`, `Swimming`, `Gym`, `Sport`, `HomeAndGarden`, `Everyday`.
Stored as string via `HasConversion<string>().HasMaxLength(50)`.

### ActivitySummary

`ActiveDays` `int`, `TotalMinutes` `int`, `TotalKilocalories` `int`, `WindowDays` `int`,
`PreviousWindowActiveDays` `int`, `PreviousWindowMinutes` `int`. Derived, never stored — the
previous-window figures are what let User Story 4 say how this week compares.

---

## Domain services

### EnergyEstimator

`Estimate(decimal met, int durationMinutes, decimal weightKg) -> int`

```
kcal = met × weightKg × (durationMinutes / 60)
```

Rounded away from zero, with a floor of **1 kcal** for any session that was actually recorded, so a
saved entry never displays "0 kcal" (R-010). Pure — no clock, no dependencies — and tested directly
at boundary values.

### ActivitySummaryCalculator

`Summarise(IReadOnlyList<ExerciseDaySummary> days, DateOnly planStart, DateTime? asOf = null) -> ActivitySummary`

Counts active days and totals over the current 7-day window and the one before it. Days before
`planStart` or after `asOf` are excluded entirely (FR-023). A member with no activity gets zeros,
not an error (FR-024).

---

## What this feature must NOT touch

Stated explicitly because it is the feature's central guarantee, and the easiest thing for an
implementer to break by being helpful:

- **`DietPlan.Targets`** — recorded exercise never changes the target in force (FR-015, FR-018).
- **`DietPlan.ActivityLevel`** — never adjusted from logged exercise (FR-018).
- **`LoggedDay.TargetSnapshot`** — never re-written; a day's target is fixed at logging time.
- **`LoggedDay.Assess()`** — its inputs stay food-only, so a day's verdict cannot move when exercise
  is added, edited or removed, including days later (FR-015, SC-008).
- **`TargetSuggestionService`** — its inputs are unchanged; logged exercise is not one of them.

`EnergyEstimator` reads the member's current weight from `DietPlan.CurrentWeightKg`. That is a read,
in the same bounded context, and the only contact point between the two aggregates.

---

## Persistence notes

- Two new `DbSet`s: `ExerciseDays`, `ActivityTypes`. `ExerciseEntry` is reached only through
  `OwnsMany` and gets no `DbSet`.
- `ExerciseDay.Version` is configured `.IsConcurrencyToken()`; SQLite has no native row version, so
  the aggregate reassigns a fresh `Guid` on every mutation (R-006).
- Indexes: `ExerciseDays(DietPlanId, Date)` unique; `ExerciseDays(UserId, Date)` for range reads;
  `ActivityTypes(SearchName)`.
- `OwnsMany` sets `ToTable("ExerciseEntries")`, `WithOwner().HasForeignKey(...)`,
  `HasKey(e => e.Id)`, `ValueGeneratedNever()` and `Ignore(e => e.DomainEvents)`; the owning
  navigation gets `UsePropertyAccessMode(PropertyAccessMode.Field)`.
- `Met` columns use `HasPrecision(4, 1)`. Minutes and kilocalories are `int` — the only exercise
  values SQL ever aggregates (ADR 0002).

## Requirement coverage

| Requirement group | Carried by |
|--------|--------|
| FR-001 to FR-009 (recording) | `ExerciseDay`, `ExerciseEntry`, `ExerciseEntryRules`, `EnergyEstimator` |
| FR-010 to FR-012 (correcting) | `ExerciseDay.UpdateEntry/RemoveEntry`, `Version` token |
| FR-013 to FR-019 (relationship to eating) | Separate aggregate and lifecycle; the "must NOT touch" list above; contract keeps the estimate out of any spendable figure |
| FR-020 to FR-024 (seeing activity) | `ExerciseTotals`, `ActivitySummaryCalculator`, range queries on `ExerciseDays` |
| FR-025 to FR-027 (catalogue) | `ActivityType`, `ActivityCatalogueSeed`, search on `SearchName` |
| FR-028 to FR-030 (access) | `IUserService`, `.RequireAuthorization()`, `UserId` filter on every query |
