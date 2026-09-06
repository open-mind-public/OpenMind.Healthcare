# Phase 1 Data Model: Beer Days and Calendar Activity Markings

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Research**: [research.md](./research.md)

All types live in `DietApi.Domain`. The aggregate derives from `AggregateRoot`; the analytics value
objects are `record` types alongside the other analytics figures.

## Aggregate map

```text
BeerDay (aggregate root)          one per (DietPlanId, Date); no children, no version
```

`BeerDay` references `DietPlan` by `DietPlanId` (a `Guid` column, no navigation, no cross-aggregate
FK). It has no relationship to `LoggedDay` or `ExerciseDay` — three independent per-day aggregates
that share a date and cannot create or destroy one another (research.md R-001).

---

## BeerDay

**Identity**: `Id` (Guid). **Unique index**: `(DietPlanId, Date)`.

| Field | Type | Notes |
|--------|--------|--------|
| `DietPlanId` | `Guid` | No navigation property. |
| `UserId` | `Guid` | Denormalised so every query filters by owner without a join (FR-016). |
| `Date` | `DateOnly` | The calendar day. Set once at creation; a beer day is moved by unmark + re-mark, never edited. |
| `CreatedAt` / `UpdatedAt` | `DateTime` | From `AggregateRoot`. `UpdatedAt` never moves after creation. |

No `Totals`, no `Version`, no entries. The record's existence *is* the fact (FR-004).

**Methods**

| Method | Behaviour |
|--------|--------|
| `static Mark(dietPlanId, userId, date, planStartDate, asOf = null)` | Checks the two date rules, returns a new `BeerDay`, emits `BeerDayMarkedEvent`. |

Unmarking is a repository delete; there is no instance method, because there is no state to
transition — the aggregate is simply removed.

**Rules** (`Domain/Rules/BeerDayRules.cs`)

| Rule | Broken when | Requirement |
|--------|--------|--------|
| `BeerDateCannotBeInFutureRule` | `date > DateOnly.FromDateTime(asOf)` | FR-003 |
| `BeerDateCannotPrecedePlanStartRule` | `date < planStartDate` | FR-003 |

Mirrors `ExerciseDateCannotBeInFutureRule` / `ExerciseDateCannotPrecedePlanStartRule`, kept
beer-specific so each has its own name and its own domain test.

---

## Events

`BeerDayMarkedEvent(Guid BeerDayId, DateOnly Date) : IDomainEvent` — raised by `Mark`. Carries
nothing that could be read as a calorie figure. No handler in this context reacts to it by touching a
target, a logged day, or a streak; the event exists for symmetry with `ExerciseLoggedEvent` and for
anything that later wants to observe the habit.

---

## Analytics value objects (`Domain/ValueObjects`)

### EatingOutcome

The eating-state split for one group of days.

| Field | Type | Notes |
|--------|--------|--------|
| `Days` | `int` | Days in the group. |
| `OnTargetDays` / `OverTargetDays` / `NotLoggedDays` | `int` | Sum to `Days`. |
| `OnTargetShare` / `OverTargetShare` / `NotLoggedShare` | `decimal` | Fraction of `Days`, 0 when `Days == 0`. |

`static From(int onTarget, int overTarget, int notLogged)` computes `Days` and the shares. A group
with no days is all zeros, never a divide-by-zero (FR-015).

### HabitAnalysis

| Field | Type | Notes |
|--------|--------|--------|
| `InPlanDays` | `int` | Days in the resolved period that fall on/after plan start and on/before today. The denominator for the per-week rates. |
| `BeerDays` | `int` | Beer days in the period, within plan. |
| `BeerDaysPerWeek` | `decimal` | `BeerDays / (InPlanDays / 7)`, 0 when `InPlanDays == 0`. Rounded to one place. |
| `ExerciseDays` | `int` | Days with recorded exercise in the period, within plan. |
| `ExerciseDaysPerWeek` | `decimal` | As above. |
| `OnBeerDays` | `EatingOutcome` | The split for the beer days. |
| `OnNonBeerDays` | `EatingOutcome` | The split for every other in-plan day. |

Both `record` types with value semantics; no EF mapping (never stored).

---

## Domain service

### HabitAnalyser (`Domain/Services/HabitAnalyser.cs`)

```
Analyse(AnalysisPeriod period,
        DateOnly planStart,
        DateOnly today,
        IReadOnlyList<DayIntakeRow> loggedDays,
        IReadOnlySet<DateOnly> beerDates,
        IReadOnlySet<DateOnly> exerciseDates) -> HabitAnalysis
```

- In-plan days = each date in `[period.From, period.To]` with `date >= planStart && date <= today`.
- Each in-plan day's state: `OverTarget` / `OnTarget` from its `DayIntakeRow` (via
  `DayAssessment.For(date, calories, target, hasEntries: true)`) if it has one, else `NotLogged`.
- `beerDates` / `exerciseDates` are intersected with the in-plan set before counting, so a stray
  date from an older plan cannot inflate a figure (mirrors the exercise range's plan clamp).
- `OnBeerDays` splits the in-plan days whose date is in `beerDates`; `OnNonBeerDays` the rest.

Pure — no clock, no repository. `today` and `planStart` are parameters (Principle IV). Tested
directly at boundaries: zero beer days, every day a beer day, a period narrower than a week, a beer
date outside the plan.

---

## Persistence notes

- One new `DbSet<BeerDay> BeerDays`. No owned collection, so no child table.
- `modelBuilder.Entity<BeerDay>`: `HasKey(e => e.Id)`; `DietPlanId`, `UserId`, `Date`, `CreatedAt`,
  `UpdatedAt` required; `HasIndex(e => new { e.DietPlanId, e.Date }).IsUnique()`;
  `HasIndex(e => new { e.UserId, e.Date })` for the range read; `Ignore(e => e.DomainEvents)`.
- Migration `AddBeerDays` — one `CREATE TABLE`, two indexes. No seed (a beer day is member data).

## Requirement coverage

| Requirement group | Carried by |
|--------|--------|
| FR-001 to FR-003 (marking) | `BeerDay.Mark`, `BeerDayRules`, `MarkBeerDay` / `UnmarkBeerDay` handlers |
| FR-004 (fact only) | `BeerDay` has no amount and no calorie field; no handler reads a `LoggedDay` |
| FR-005 to FR-009 (calendar markings) | frontend: `diet-calendar` beer + exercise indicators, legend, both views; `--beer-mark` / `--exercise-mark` tokens |
| FR-010 (no effect on existing figures) | no shared state; `BeerDay` never loaded by stats, streak, or averages |
| FR-011 to FR-015 (analytics) | `HabitAnalyser`, `HabitAnalysis`, `EatingOutcome`, `GET /api/diet-analytics/habits` |
| FR-016, FR-017 (access, one per date) | `IUserService`, `.RequireAuthorization()`, `UserId` filter, unique index, idempotent mark |
