# Phase 1 Data Model: Diet Tracking

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Research**: [research.md](./research.md)

All types live in `DietApi.Domain`. Aggregates derive from `AggregateRoot`, value objects from
`ValueObject`, both in `Shared/DDD.BuildingBlocks`. Every mapped entity gets
`entity.Ignore(e => e.DomainEvents)`.

## Aggregate map

```text
DietPlan (aggregate root)                    one per member, keyed by UserId
├── BodyMetrics                  owned VO
├── NutritionTargets             owned VO      the targets currently in force
├── WeightReading[]              owned entity  at most one per date
└── UnlockedAchievement[]        owned entity  at most one per achievement

LoggedDay (aggregate root)                   one per (DietPlanId, Date)
├── NutritionTargets             owned VO      snapshot taken when the day was created
├── NutritionValues              owned VO      persisted totals, recomputed on every change
└── FoodEntry[]                  owned entity  many per day
    └── NutritionValues          owned VO      snapshot taken when the entry was logged

FoodLibraryItem (aggregate root)             seeded reference data
└── ServingSize[]                owned entity
    └── NutritionValues          owned VO

DietAchievement (aggregate root)             seeded reference data
EatingTip (aggregate root)                   seeded reference data
```

`LoggedDay` references `DietPlan` by `DietPlanId` (a `Guid` column, no navigation property, no
foreign key constraint across the aggregate boundary). `FoodEntry` keeps `FoodLibraryItemId` and
`ServingSizeId` for provenance only — it never re-reads them to compute anything (R-005).

---

## DietPlan

**Root of**: weight readings, unlocked achievements. **Identity**: `Id` (Guid), unique index on
`UserId`.

| Field | Type | Notes |
|--------|--------|--------|
| `UserId` | `Guid` | From the JWT only (FR-043). Unique index — one active plan per member (FR-001). |
| `Goal` | `GoalType` | `LoseWeight`, `Maintain`, `GainWeight`, `EatConsistently`. Stored as string. |
| `StartDate` | `DateOnly` | Not in the future (FR-002). |
| `BodyMetrics` | `BodyMetrics` | Owned. Height, age, sex. |
| `ActivityLevel` | `ActivityLevel` | `Sedentary`, `LightlyActive`, `ModeratelyActive`, `VeryActive`, `ExtraActive`. Stored as string. |
| `Targets` | `NutritionTargets` | Owned. Currently in force. |
| `TargetSource` | `TargetSource` | `Suggested` or `MemberSet` (FR-006). Stored as string. |
| `TargetWeightKg` | `decimal(5,2)?` | Optional (FR-012 group). |
| `WeightReadings` | `IReadOnlyCollection<WeightReading>` | Owned, private backing field. |
| `UnlockedAchievements` | `IReadOnlyCollection<UnlockedAchievement>` | Owned, private backing field. |
| `CreatedAt` / `UpdatedAt` | `DateTime` | From `AggregateRoot`. |

**Methods**

| Method | Behaviour |
|--------|--------|
| `static Create(userId, goal, startDate, bodyMetrics, activityLevel, targets, targetSource, targetWeightKg, asOf)` | Checks start-date and target rules, emits `DietPlanCreatedEvent`. |
| `UpdatePlan(goal, startDate, bodyMetrics, activityLevel, targetWeightKg, asOf)` | Changes everything except targets. Leaves `Targets` untouched (FR-009). |
| `SetTargets(targets, source)` | The only way targets change. Records the source. Emits `TargetsChangedEvent`. |
| `RecordWeight(date, weightKg, asOf)` | Replaces any reading on that date (FR-012). Returns the reading. |
| `RemoveWeightReading(date)` | Returns `bool`. |
| `CurrentWeightKg(asOf)` | The most recent reading at or before `asOf`, else `BodyMetrics` has no weight — falls back to the weight supplied at setup, held as the first reading (R-001, FR-017). |
| `WeightTrend(from, to, asOf)` | Returns `WeightTrend`: ordered readings, change since `StartDate`, remaining to target, `GoalReached` (FR-015, FR-016). |
| `Unlock(achievementId, earnedOn)` | No-op if already unlocked — never revokes, never duplicates (FR-039). |

**Rules** (`Domain/Rules/DietPlanRules.cs`)

| Rule | Broken when | Requirement |
|--------|--------|--------|
| `PlanStartDateCannotBeInFutureRule` | `startDate > DateOnly.FromDateTime(asOf)` | FR-002 |
| `DailyCalorieTargetMustBePositiveRule` | `calories <= 0` | FR-003 |
| `HeightMustBePlausibleRule` | outside 50-250 cm | FR-005 inputs |
| `AgeMustBePlausibleRule` | outside 13-120 years | FR-005 inputs |
| `TargetWeightMustBePlausibleRule` | set and outside 20-500 kg | FR-014 |

---

## LoggedDay

**Root of**: food entries. **Identity**: `Id` (Guid), unique index on `(DietPlanId, Date)`.

Created lazily — the first entry for a date creates the day; deleting the last entry deletes it
(R-008). A day never exists with zero entries.

| Field | Type | Notes |
|--------|--------|--------|
| `DietPlanId` | `Guid` | No navigation property. |
| `UserId` | `Guid` | Denormalised so every query filters by owner without a join (FR-043). |
| `Date` | `DateOnly` | The calendar day. Immutable once set (FR-029). |
| `TargetSnapshot` | `NutritionTargets` | Owned. Captured at creation, never updated (R-006, FR-004). |
| `Totals` | `NutritionValues` | Owned. Recomputed on every entry change (R-010). |
| `Entries` | `IReadOnlyCollection<FoodEntry>` | Owned, private backing field. |

**Methods**

| Method | Behaviour |
|--------|--------|
| `static StartDay(dietPlanId, userId, date, targetSnapshot, planStartDate, asOf)` | Checks date rules, creates an empty day. Internal to the aggregate's first `AddEntry`. |
| `AddEntry(foodItemId, servingSizeId, foodName, servingLabel, quantity, mealType, nutritionPerServing, asOf)` | Checks quantity and ceiling rules, multiplies `nutritionPerServing × quantity`, appends, recomputes `Totals`. Emits `FoodEntryLoggedEvent`. |
| `UpdateEntry(entryId, quantity, servingSizeId, servingLabel, nutritionPerServing, mealType)` | Same rules, recomputes `Totals`. |
| `RemoveEntry(entryId)` | Recomputes `Totals`. Returns `bool`. `IsEmpty` then tells the repository to delete the day. |
| `IsEmpty` | `Entries.Count == 0`. |
| `Assess()` | `DayAssessment`: consumed, remaining, `DayState`, overage. Pure function of `Totals` and `TargetSnapshot` (FR-031, FR-033). |
| `EntriesByMeal()` | Grouped for display. |

**Rules** (`Domain/Rules/FoodEntryRules.cs`)

| Rule | Broken when | Requirement |
|--------|--------|--------|
| `EntryDateCannotBeInFutureRule` | `date > DateOnly.FromDateTime(asOf)` | FR-026 |
| `EntryDateCannotPrecedePlanStartRule` | `date < plan.StartDate` | FR-026 |
| `QuantityMustBePositiveRule` | `quantity <= 0` | FR-027 |
| `EntryCaloriesWithinCeilingRule` | entry calories > 10,000 kcal | FR-027 |

**Invariant asserted by test**: after any `AddEntry`, `UpdateEntry`, or `RemoveEntry`,
`Totals.Calories == Entries.Sum(e => e.Nutrition.Calories)` (R-010).

---

## FoodEntry (owned by LoggedDay)

| Field | Type | Notes |
|--------|--------|--------|
| `Id` | `Guid` | `ValueGeneratedNever()`. |
| `LoggedDayId` | `Guid` | Owner FK. |
| `FoodLibraryItemId` | `Guid` | Provenance only. |
| `ServingSizeId` | `Guid` | Provenance only. |
| `FoodName` | `string(200)` | Snapshotted so a renamed library food does not rewrite history. |
| `ServingLabel` | `string(50)` | Snapshotted, e.g. "1 medium". |
| `Quantity` | `decimal(6,2)` | Fractional servings supported (FR-024). |
| `MealType` | `MealType` | `Breakfast`, `Lunch`, `Dinner`, `Snack`. Stored as string. |
| `Nutrition` | `NutritionValues` | Owned. `perServing × Quantity`, snapshotted (FR-025). |
| `LoggedAt` | `DateTime` | UTC. |

---

## WeightReading (owned by DietPlan)

| Field | Type | Notes |
|--------|--------|--------|
| `Id` | `Guid` | `ValueGeneratedNever()`. |
| `DietPlanId` | `Guid` | Owner FK. |
| `Date` | `DateOnly` | Unique index on `(DietPlanId, Date)` (FR-012). |
| `WeightKg` | `decimal(5,2)` | Never SQL-aggregated or SQL-ordered (R-010). |
| `RecordedAt` | `DateTime` | UTC. |

**Rules** (`Domain/Rules/WeightReadingRules.cs`)

| Rule | Broken when | Requirement |
|--------|--------|--------|
| `WeightDateCannotBeInFutureRule` | `date > DateOnly.FromDateTime(asOf)` | FR-014 |
| `WeightMustBePlausibleRule` | outside 20-500 kg | FR-014 |

---

## UnlockedAchievement (owned by DietPlan)

| Field | Type | Notes |
|--------|--------|--------|
| `Id` | `Guid` | `ValueGeneratedNever()`. |
| `DietPlanId` | `Guid` | Owner FK. |
| `DietAchievementId` | `Guid` | Unique index on `(DietPlanId, DietAchievementId)` (FR-039). |
| `EarnedOn` | `DateOnly` | The date the criteria were met (FR-038). |

---

## FoodLibraryItem (seeded reference data)

| Field | Type | Notes |
|--------|--------|--------|
| `Id` | `Guid` | |
| `Name` | `string(200)` | Display name. |
| `SearchName` | `string(200)` | Lowercased, accent-stripped. Indexed. Search matches on this (R-009). |
| `Category` | `FoodCategory` | `Staple`, `Protein`, `Dairy`, `Fruit`, `Vegetable`, `PreparedMeal`, `Snack`, `Drink`. Stored as string. |
| `ServingSizes` | `IReadOnlyCollection<ServingSize>` | Owned, at least one. |

### ServingSize (owned)

| Field | Type | Notes |
|--------|--------|--------|
| `Id` | `Guid` | `ValueGeneratedNever()`. |
| `Label` | `string(50)` | "100 g", "1 medium", "1 cup". |
| `GramWeight` | `decimal(7,2)` | For display and comparison. |
| `Nutrition` | `NutritionValues` | Owned. Values *for this serving* (R-009). |

**Seed**: 150-200 items across the eight categories, guarded by
`if (!context.FoodLibraryItems.Any())` (FR-021, SC-011).

---

## DietAchievement (seeded reference data)

| Field | Type | Notes |
|--------|--------|--------|
| `Id` | `Guid` | |
| `Name` | `string(100)` | |
| `Description` | `string(500)` | |
| `Icon` | `string(10)` | Emoji, matching the smoking area's convention. |
| `Criterion` | `AchievementCriterion` | `ConsecutiveOnTargetDays`, `TotalDaysLogged`, `DaysOnPlan`. Stored as string. |
| `Threshold` | `int` | Days required. |

**Seed** (guarded): first day logged (`TotalDaysLogged` 1), a full week on target
(`ConsecutiveOnTargetDays` 7), two weeks (14), a full month (30), thirty days logged
(`TotalDaysLogged` 30), one hundred logged (100), a month on plan (`DaysOnPlan` 30), a
hundred days on plan (100).

---

## EatingTip (seeded reference data)

| Field | Type | Notes |
|--------|--------|--------|
| `Id` | `Guid` | |
| `Title` | `string(100)` | |
| `Description` | `string(500)` | |
| `Icon` | `string(10)` | |
| `Category` | `TipCategory` | `Craving`, `Planning`, `PortionControl`, `EatingOut`, `Mindset`. Stored as string. |

---

## Value objects

### NutritionValues

`Calories` `int` (whole kcal — R-010), `ProteinG` `decimal(6,1)`, `CarbsG` `decimal(6,1)`,
`FatG` `decimal(6,1)`. Static factories `Create(...)` and `Zero()`. Operators or methods for
`Plus`, `Times(decimal quantity)`. Equality over all four components.

### NutritionTargets

`Calories` `int`, `ProteinG` `decimal?`, `CarbsG` `decimal?`, `FatG` `decimal?` — macros optional
(spec Assumptions). Static `Create(...)` checking `DailyCalorieTargetMustBePositiveRule`.

### BodyMetrics

`HeightCm` `decimal(5,1)`, `Age` `int`, `Sex` `BiologicalSex` (`Female`, `Male`). Static `Create`
checking the height and age rules. Weight is deliberately *not* here — it lives in
`WeightReadings` so there is one source of truth for current weight (FR-017).

### DayAssessment

`Date`, `ConsumedCalories` `int`, `TargetCalories` `int`, `RemainingCalories` `int` (negative when
over), `State` `DayState`, `OverageCalories` `int`. Derived, not stored.

### DietStatistics

`CurrentStreakDays`, `LongestStreakDays`, `TotalDaysLogged`, `AverageDailyCalories`,
`WindowDays`. Derived by `StreakCalculator` from an ordered list of `(DateOnly, DayState)`
covering the plan range up to `asOf`.

### WeightTrend

`Readings` (ordered), `StartWeightKg`, `CurrentWeightKg`, `ChangeKg`, `TargetWeightKg`,
`RemainingToTargetKg`, `GoalReached` `bool`.

### TargetSuggestion

`SuggestedTargets` `NutritionTargets`, `RestingEnergyKcal` `int`, `ActivityAdjustedKcal` `int`,
`GoalAdjustmentKcal` `int`, `WasClampedToFloor` `bool`, `FloorKcal` `int`. Every intermediate is
exposed so the UI can explain the number rather than assert it (FR-010).

---

## Domain services

### TargetSuggestionService

`Suggest(BodyMetrics, currentWeightKg, ActivityLevel, GoalType) -> TargetSuggestion`

Mifflin-St Jeor → activity factor → goal adjustment → clamp to floor → macro split (R-001, R-002,
R-003). Pure, no dependencies, no clock. Tested directly at every boundary: each sex, each
activity level, each goal, and the clamp.

### StreakCalculator

`Calculate(IReadOnlyList<(DateOnly Date, DayState State)> days, DateOnly planStart, DateTime? asOf = null) -> DietStatistics`

Current streak counts back from `asOf`'s date through consecutive `OnTarget` days, stopping at the
first day that is `OverTarget` or `NotLogged` (R-008). Longest streak scans the whole range. Days
before `planStart` or after `asOf` are excluded entirely (FR-036). Average intake is over the most
recent 30 days that were logged.

### DietAchievementStatusService

`Evaluate(DietPlan, DietStatistics, IReadOnlyList<DietAchievement>, asOf) -> IReadOnlyList<DietAchievementStatus>`

Marks an achievement unlocked if the plan already holds an `UnlockedAchievement` for it —
persisted state wins, always (FR-039) — otherwise checks the criterion against the statistics and,
when met, calls `plan.Unlock(...)`. Locked entries carry the remaining count (FR-040).

---

## Persistence notes

- `DietDbContext` copies the `SaveChangesAsync` override from `AppDbContext`: collect domain events,
  clear them, save, then publish through MediatR.
- `DbSet`s: `DietPlans`, `LoggedDays`, `FoodLibraryItems`, `DietAchievements`, `EatingTips`.
  `WeightReading`, `UnlockedAchievement`, `FoodEntry`, and `ServingSize` are reached only through
  `OwnsMany` and get no `DbSet`.
- Every `OwnsMany` sets `ToTable(...)`, `WithOwner().HasForeignKey(...)`, `HasKey(d => d.Id)`,
  `Property(d => d.Id).ValueGeneratedNever()`, and `Ignore(d => d.DomainEvents)`; the owning
  navigation gets `UsePropertyAccessMode(PropertyAccessMode.Field)`.
- Enums are `HasConversion<string>().HasMaxLength(50)` so they read the same way as
  `RelapseTrigger` does today.
- Indexes: `DietPlans(UserId)` unique; `LoggedDays(DietPlanId, Date)` unique;
  `LoggedDays(UserId, Date)` for range reads; `WeightReadings(DietPlanId, Date)` unique;
  `UnlockedAchievements(DietPlanId, DietAchievementId)` unique;
  `FoodLibraryItems(SearchName)`.
- Decimal columns use `HasPrecision(...)` as `Money` does. Calorie columns are `int` and are the
  only nutrition values SQL ever aggregates (R-010).

## Requirement coverage

| Requirement group | Carried by |
|--------|--------|
| FR-001 to FR-011 (plan, suggestion, floor, override) | `DietPlan`, `TargetSuggestionService`, `DietPlanRules` |
| FR-012 to FR-018 (weight) | `WeightReading`, `DietPlan.RecordWeight/WeightTrend`, `WeightReadingRules` |
| FR-019 to FR-022 (library) | `FoodLibraryItem`, `ServingSize`, `DbInitializer` |
| FR-023 to FR-030 (logging) | `LoggedDay`, `FoodEntry`, `FoodEntryRules` |
| FR-031 to FR-033 (assessment) | `LoggedDay.Assess`, `DayAssessment`, `NutritionValues` |
| FR-034 to FR-037 (history) | `StreakCalculator`, `DietStatistics`, range queries on `LoggedDays` |
| FR-038 to FR-041 (recognition, guidance) | `DietAchievement`, `UnlockedAchievement`, `DietAchievementStatusService`, `EatingTip` |
| FR-042 to FR-044 (access, separation) | `UserService`, `.RequireAuthorization()`, own `DietDbContext` and database |
