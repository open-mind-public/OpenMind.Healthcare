# Phase 1 Data Model: Diet Analytics

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Research**: [research.md](./research.md)

**Nothing here is persisted.** This feature adds no table, no column and no migration. Every type
below is a value object computed on demand, or a flat row returned by a read query. That is why
there is no aggregate map: there is no aggregate.

All types live in `DietApi.Domain`. Value objects derive from `ValueObject`, are immutable, and are
built through static factories.

---

## The read model

`IDietAnalyticsRepository` returns records with no behaviour. It does the arithmetic SQL is good
at and nothing else; every judgement is applied afterwards, in the domain (research R-009).

| Row | Shape | Source | Notes |
|--------|--------|--------|--------|
| `DayIntakeRow` | `Date`, `Calories`, `ProteinG`, `CarbsG`, `FatG`, `TargetCalories`, `TargetProteinG`, `TargetCarbsG`, `TargetFatG` | one per logged day in range | Macronutrients travel as `decimal` and are summed **in memory** (R-005). Targets are the ones stored on that day, which is what makes FR-011 possible. |
| `MealIntakeRow` | `MealType`, `Kilocalories`, `EntryCount` | `GROUP BY` meal | Energy only; `int`, aggregated in SQL. |
| `FoodContributionRow` | `FoodLibraryItemId`, `FoodName`, `Kilocalories`, `Times` | `GROUP BY` food, ordered by kcal, `Take(n)` | Ordered by an `int`, never by a decimal (R-007). |
| `CategoryIntakeRow` | `Category`, `Kilocalories` | join to `FoodLibraryItems` | Category is not on the entry; it comes from the library (R-004). |
| `QuarterHourRow` | `Hour` (0–23), `Quarter` (0–3), `Kilocalories` | `GROUP BY` hour and minute/15 | 96 buckets, rotated by the caller's offset (R-006). |

Every query filters on `UserId` and a date range, so another member's data is unreachable rather
than merely forbidden — the pattern the rest of the service already uses.

---

## AnalysisPeriod

The resolved window everything else is computed against. Produced by `AnalysisPeriodResolver` from
a preset, the member's plan start and `asOf`; consumed by every other type here.

| Field | Type | Notes |
|--------|--------|--------|
| `Preset` | `PeriodPreset` | What was asked for. |
| `From` / `To` | `DateOnly` | What is actually analysed, after clamping to the plan and to today. |
| `WasNarrowed` | `bool` | True when clamping changed the requested window. FR-002 requires saying so. |
| `TotalDays` | `int` | Calendar days in `[From, To]`. |
| `LoggedDays` | `int` | Days in the window carrying at least one entry (R-011). |
| `PreviousFrom` / `PreviousTo` | `DateOnly?` | The preceding span of the same length, clamped. Null for the whole-plan preset, and for any window with no room before it. |
| `HasComparison` | `bool` | False rather than a window of zeros — zeros would assert something about a period that does not exist (R-012). |

**Rules**

| Rule | Broken when | Requirement |
|--------|--------|--------|
| `PeriodMustFallWithinPlanRule` | `From < planStart` or `To > today` after resolution | FR-002 |
| `PeriodMustNotBeEmptyRule` | `To < From` | FR-002 |

`PeriodPreset`: `Week` (7 days), `Month` (30), `Quarter` (90), `Plan` (from plan start). Stored
nowhere; crosses the wire as a name.

---

## IntakeSummary (US1)

| Field | Type | Notes |
|--------|--------|--------|
| `TotalKilocalories` | `int` | Over logged days in the period. |
| `AverageDailyKilocalories` | `int` | **Over logged days**, not calendar days. |
| `AveragedOverDays` | `int` | The denominator, carried with the figure so it cannot be separated from it (FR-003, SC-003). |
| `OnTargetDays` / `OverTargetDays` / `NotLoggedDays` | `int` | Over **all** days in the period — the question there is how many were missed. Sums to `AnalysisPeriod.TotalDays`. |
| `PreviousAverageDailyKilocalories` | `int?` | Null when `HasComparison` is false. |

**Invariant asserted by test**: `OnTargetDays + OverTargetDays + NotLoggedDays == TotalDays`.

> Two different denominators appear in one type, deliberately. An intake average over days the
> member did not log would be a lie; a count of missed days that ignored them would be useless.
> Both are labelled, which is the whole point of FR-003.

## MealBreakdown, CategoryBreakdown, FoodContribution (US1)

| Type | Fields | Notes |
|--------|--------|--------|
| `MealBreakdown` | one `MealShare` per `MealType`: `Meal`, `Kilocalories`, `ShareOfTotal`, `EntryCount` | **Invariant**: shares sum to the period total (FR-006, SC-002). Meals with nothing logged appear at zero rather than being absent, so the four always add up. |
| `CategoryBreakdown` | one `CategoryShare` per `FoodCategory`: `Category`, `Kilocalories`, `ShareOfTotal` | Same invariant. Categories come from the library join (R-004). |
| `FoodContribution` | `FoodLibraryItemId`, `FoodName`, `Kilocalories`, `ShareOfTotal`, `TimesLogged` | Top ten by energy. Deliberately **not** exhaustive, so these do not sum to the total and the type does not pretend they do. |

Shares are computed in the domain as a percentage of the period total, rounded to one decimal
place, with the largest remainder absorbing rounding drift so the displayed parts still sum to 100.

## MacronutrientComparison (US2)

| Field | Type | Notes |
|--------|--------|--------|
| `ProteinG` / `CarbsG` / `FatG` | `decimal` | Summed **in memory** across `DayIntakeRow` (R-005). |
| `TargetProteinG` / `TargetCarbsG` / `TargetFatG` | `decimal?` | Summed from each day's own stored target (FR-011). Null when the plan carries no macronutrient targets. |
| `ProteinShare` / `CarbsShare` / `FatShare` | `decimal` | Share of energy, from the standard 4/4/9 kcal per gram. |
| `HasTargets` | `bool` | False means present the split and compare it to nothing (FR-012). |
| `AveragedOverDays` | `int` | Same denominator discipline as `IntakeSummary`. |

> The targets are summed per day rather than taken from the plan, so a period spanning a target
> change is judged against what was actually in force each day. Testing that is SC-006.

## WeekdayDistribution and TimeOfDayDistribution (US3)

| Type | Fields | Notes |
|--------|--------|--------|
| `WeekdayDistribution` | seven `WeekdayShare`: `DayOfWeek`, `AverageKilocalories`, `LoggedDays` | Derived from `DayIntakeRow.Date`, a `DateOnly` — no timezone involved. Average per weekday over the days of that weekday actually logged, with the count carried. |
| `TimeOfDayDistribution` | 24 `HourShare`: `Hour`, `Kilocalories`, `ShareOfTotal`; plus `UtcOffsetMinutes` | Rotated from the 96 quarter-hour buckets by the caller's offset, so +05:30 and +05:45 land exactly (R-006). |

Both carry `IsApproximate = true` and the reason: the time recorded is when the entry was logged,
not necessarily when the food was eaten (FR-015).

---

## Observation and the rules that produce it (US4)

### Observation

| Field | Type | Notes |
|--------|--------|--------|
| `Family` | `ObservationFamily` | What FR-022 de-duplicates on. |
| `Text` | `string` | Fixed wording with the figure interpolated. Never generated freely (R-010). |
| `Figure` | `string` | The number it rests on, carried separately so the screen can emphasise it and a test can assert it (FR-017). |
| `Strength` | `decimal` | How far past its threshold the rule fired, normalised to 0–1. Orders the list, and makes FR-020 determinism a property of the arithmetic. |
| `BasedOnDays` | `int` | The logged days behind it (FR-017). |

`ObservationFamily`: `Timing`, `Composition`, `Targets`, `Consistency`.

### IObservationRule

```
ObservationFamily Family        # for de-duplication
int MinimumLoggedDays           # declared, so SC-008 can assert it generically
string ThresholdDescription     # for the release review against FR-019
Observation? Evaluate(AnalyticsFigures figures)
```

`Evaluate` returns null when the threshold is not met. The engine never calls it when
`figures.Period.LoggedDays < MinimumLoggedDays`, so a rule cannot accidentally fire on thin data
even if its own arithmetic would allow it.

### The seven rules for this release

| Rule | Family | Min. logged days | Fires when | Example wording |
|--------|--------|--------|--------|--------|
| `LateEatingRule` | Timing | 14 | ≥ 25% of energy logged after 21:00 local | "About a third of what you logged this month was after 9pm." |
| `WeekendHeavierRule` | Timing | 14, with ≥ 2 weekend and ≥ 4 weekday days | Weekend daily average exceeds weekday average by ≥ 20% | "Your Saturdays and Sundays averaged 620 kcal above your weekdays." |
| `SingleFoodDominanceRule` | Composition | 14 | One food ≥ 15% of period energy | "Porridge oats accounted for 18% of everything you logged." |
| `MealSkewRule` | Composition | 14 | One meal ≥ 45% of period energy | "Dinner was 47% of your intake this month." |
| `LowPlantShareRule` | Composition | 14 | Fruit + vegetables < 10% of energy | "Fruit and vegetables came to 6% of what you logged." |
| `ProteinBelowTargetRule` | Targets | 14, and the plan has macronutrient targets | Average protein ≤ 80% of average target | "You averaged 96 g of protein against a target of 130 g." |
| `LoggingImprovedRule` | Consistency | 14 in both windows | Logged days up ≥ 25% on the previous window | "You logged 24 days this month, up from 17." |

Every one states a figure about the member's own data. None diagnoses a condition, calls the
member's eating good or bad, or tells them what to eat — which is FR-019, and which SC-010 checks
line by line before release.

### ObservationEngine

`Observe(AnalyticsFigures figures) -> IReadOnlyList<Observation>`

1. Discard rules whose `MinimumLoggedDays` exceeds the period's logged days (FR-018).
2. Evaluate the rest; discard nulls (FR-018, threshold half).
3. Keep only the strongest per `Family` (FR-022).
4. Order by `Strength` descending, then by `Family` for a stable tie-break (FR-020).
5. An empty result is a valid answer and means "nothing stood out" (FR-021).

Pure: no clock, no repository, no randomness. Given the same `AnalyticsFigures` it returns the same
list, which is FR-020 by construction rather than by discipline.

---

## What this feature must NOT do

Stated explicitly because analytics is the most natural place in the application for someone to
break these while trying to be helpful:

- **No combined figure.** Nothing here subtracts exercise energy from intake, adds it to a target,
  or produces a "net" or "available" number (FR-023, R-014). A structural test asserts no analytics
  response type carries such a field.
- **No writes.** No logged day, target, assessment or plan is altered by viewing analytics
  (FR-024). Every repository method on the read model is a query.
- **No re-judging.** A day's target is read from that day's stored snapshot, never from the plan's
  current target (FR-011).
- **No inventing data.** A day the member did not log is an absence, not a zero, in every average
  (R-011). A period with no preceding window reports no comparison, not a comparison to zero.

## Requirement coverage

| Requirement group | Carried by |
|--------|--------|
| FR-001 to FR-004 (choosing a period) | `AnalysisPeriod`, `AnalysisPeriodResolver`, `PeriodPreset` |
| FR-005 to FR-009 (where calories go) | `IntakeSummary`, `MealBreakdown`, `FoodContribution`, `CategoryBreakdown` |
| FR-010 to FR-012 (targets and macros) | `MacronutrientComparison`, per-day targets on `DayIntakeRow` |
| FR-013 to FR-015 (patterns) | `WeekdayDistribution`, `TimeOfDayDistribution` |
| FR-016 to FR-022 (observations) | `Observation`, `IObservationRule`, the seven rules, `ObservationEngine` |
| FR-023 to FR-027 (boundaries and access) | The "must NOT" list above; `IUserService` and `.RequireAuthorization()`; read-only queries |
