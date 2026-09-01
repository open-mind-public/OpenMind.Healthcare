# Phase 1 Data Model: Diet Subdomain

**Feature**: `001-diet-subdomain` | **Date**: 2026-09-01

Derived from the Key Entities in [spec.md](./spec.md) and the aggregate boundaries decided in
[research.md](./research.md#r8-aggregate-boundaries). All types live in `DietApi/Domain/` and follow
Constitution Principle II: private setters, private parameterless constructor for EF Core, static
factory methods, invariants enforced through `IBusinessRule`.

## Aggregate map

```
DietProfile (AR)  ──owns──▶ NutritionTarget (VO, current)
                  ──owns──▶ TargetRecord (entity, history)
                  ──owns──▶ WeightGoal (VO, nullable)
                  ──owns──▶ WeighIn (entity, 0..n, one per date)

FoodLogDay (AR)   ──owns──▶ NutritionTarget (VO, snapshot of the day's target)
                  ──owns──▶ LoggedEntry (entity, 0..n)
                                └─owns─▶ Quantity (VO)
                                └─owns─▶ NutritionSnapshot (VO)

Food (AR)         ──owns──▶ NutritionFacts (VO, per 100 reference units)

Recipe (AR)       ──owns──▶ RecipeIngredient (entity, 1..n)

MealPlan (AR)     ──owns──▶ PlannedMeal (entity, 0..n)
```

No navigation property crosses an aggregate boundary. References between aggregates are `Guid`
values only, resolved by the query handler that needs them.

---

## Enumerations

| Enum | Values | Notes |
|---|---|---|
| `Sex` | `Female`, `Male` | Used only as an input to Mifflin-St Jeor and the safety floor. Persisted as string. |
| `ActivityLevel` | `Sedentary`, `LightlyActive`, `ModeratelyActive`, `VeryActive`, `ExtraActive` | Factors 1.2 / 1.375 / 1.55 / 1.725 / 1.9 |
| `GoalDirection` | `Lose`, `Maintain`, `Gain` | |
| `UnitSystem` | `Metric`, `Imperial` | Presentation only; storage is always metric |
| `TargetOrigin` | `Derived`, `Manual` | FR-002 vs FR-003 |
| `MealOccasion` | `Breakfast`, `Lunch`, `Dinner`, `Snack` | FR-016 breakdown |
| `FoodCategory` | `Fruit`, `Vegetable`, `Grain`, `Protein`, `Dairy`, `Fat`, `Beverage`, `Prepared` | Catalog grouping |
| `MeasurementUnit` | `Gram`, `Millilitre`, `Piece` | Consolidation key for the shopping list |

All enums are persisted with `.HasConversion<string>().HasMaxLength(50)` and serialised by name via
the existing `JsonStringEnumConverter` convention.

---

## Value objects

### `NutritionFacts`

Energy and macronutrients for **100** reference units of a food.

| Field | Type | Rule |
|---|---|---|
| `EnergyKcal` | `decimal` | ≥ 0 |
| `ProteinG` | `decimal` | ≥ 0 |
| `CarbohydrateG` | `decimal` | ≥ 0 |
| `FatG` | `decimal` | ≥ 0 |

`Create(...)` rejects negatives. `Scale(decimal factor)` returns scaled facts — the single
multiplication that makes SC-003 exact. Rounded to 2 decimal places on construction, matching the
rounding discipline in `Money`.

### `Quantity`

| Field | Type | Rule |
|---|---|---|
| `Amount` | `decimal` | > 0 and ≤ 5000 (FR-018 plausibility ceiling) |
| `Unit` | `MeasurementUnit` | |

`Add(Quantity other)` throws `DomainException` on differing units — the shopping list must group
before summing rather than rely on this throwing (FR-030).

### `NutritionSnapshot`

The frozen record of what an entry contributed, per
[R7](./research.md#r7-keeping-logged-history-immune-to-later-edits).

| Field | Type | Notes |
|---|---|---|
| `FoodName` | `string` (≤ 200) | Copied at log time; survives deletion of the food |
| `Facts` | `NutritionFacts` | Already scaled to the logged quantity, **not** per 100 |

### `NutritionTarget`

| Field | Type | Rule |
|---|---|---|
| `EnergyKcal` | `decimal` | > 0 |
| `ProteinPercent` | `int` | 0-100 |
| `CarbohydratePercent` | `int` | 0-100 |
| `FatPercent` | `int` | 0-100; the three MUST sum to exactly 100 |
| `Origin` | `TargetOrigin` | |

Derived members `ProteinG`, `CarbohydrateG`, `FatG` apply Atwater factors (4/4/9) to the percentage
split. `IsBelowSafeFloor(Sex)` compares `EnergyKcal` against 1200 (female) / 1500 (male).

### `WeightGoal`

| Field | Type | Rule |
|---|---|---|
| `TargetWeightKg` | `decimal` | 20-500 |
| `Direction` | `GoalDirection` | |
| `WeeklyRateKg` | `decimal` | 0 for `Maintain`; otherwise 0 < rate ≤ 1.0 |
| `SetOn` | `DateOnly` | |
| `AchievedOn` | `DateOnly?` | Set once reached (FR-024) |

### `BodyMeasurements`

`HeightCm` (50-260) and `StartingWeightKg` (20-500), owned by the profile.

---

## Aggregate: `DietProfile`

One per person. The consistency boundary for identity, targets, and weight (FR-007, FR-020, FR-021).

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | from `Entity` |
| `UserId` | `Guid` | unique index; from JWT only (Principle I) |
| `StartedOn` | `DateOnly` | bounds the tracked period (FR-017, FR-019) |
| `DateOfBirth` | `DateOnly` | FR-039 — held here, not in the account record |
| `Sex` | `Sex` | |
| `Measurements` | `BodyMeasurements` | owned |
| `ActivityLevel` | `ActivityLevel` | |
| `UnitSystem` | `UnitSystem` | FR-040 |
| `CurrentTarget` | `NutritionTarget` | owned |
| `BelowFloorAcknowledgedAt` | `DateTime?` | FR-004a |
| `TargetHistory` | `IReadOnlyCollection<TargetRecord>` | owned; FR-005 |
| `Goal` | `WeightGoal?` | owned |
| `WeighIns` | `IReadOnlyCollection<WeighIn>` | owned; FR-021 |

**Behaviour**

| Method | Enforces |
|---|---|
| `static Start(userId, dateOfBirth, sex, heightCm, startingWeightKg, activityLevel, unitSystem, startedOn, asOf)` | derives the initial target; records it in history |
| `SetDerivedTarget(asOf)` | recomputes from current measurements per R1 |
| `OverrideTarget(energyKcal, protein%, carb%, fat%, asOf)` | FR-003; macro split must total 100 |
| `AcknowledgeBelowFloorTarget(asOf)` | FR-004a |
| `SetGoal(targetWeightKg, weeklyRateKg, asOf)` | FR-024 |
| `RecordWeighIn(date, weightKg, asOf)` | FR-020, FR-021, FR-025 — amends in place on a repeat date |
| `RemoveWeighIn(date)` | returns `false` when absent |
| `TargetOn(DateOnly date)` | FR-005 — the target in force on that date |
| `GetWeightProgress(asOf)` | FR-022, FR-023 — total change, windowed trend, distance to goal |

**Invariants**

- At most one `WeighIn` per `(ProfileId, Date)` — unique index plus in-aggregate amend, exactly the
  pattern `QuitJourney.MarkDayAsSmoked` uses for `SmokedDay`.
- `TargetHistory` is append-only; changing a target never rewrites a past record (FR-005).
- Changing measurements or activity level does **not** retroactively alter `TargetHistory`.

**State transitions (goal)**

```
(no goal) ──SetGoal──▶ Active ──weigh-in reaches target──▶ Achieved ──SetGoal──▶ Active
```

`Achieved` is reached by `RecordWeighIn` evaluating the goal, not by a background process.

### Entity: `TargetRecord` (owned)

`Id`, `ProfileId`, `EffectiveFrom` (`DateOnly`), `Target` (`NutritionTarget`, owned),
`RecordedAt` (`DateTime`).

### Entity: `WeighIn` (owned)

`Id`, `ProfileId`, `Date` (`DateOnly`), `WeightKg` (`decimal`), `RecordedAt` (`DateTime`).

---

## Aggregate: `Food`

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | |
| `Name` | `string` (≤ 200) | indexed for search (FR-009) |
| `Category` | `FoodCategory` | |
| `OwnerUserId` | `Guid?` | **null = shipped catalog, visible to all** (FR-008); non-null = private (FR-011) |
| `ReferenceUnit` | `MeasurementUnit` | `Gram` or `Millilitre`; facts are per 100 of it |
| `Facts` | `NutritionFacts` | owned |

**Behaviour**: `static CreateCatalogItem(...)` (owner null, used by the seeder),
`static CreateCustom(ownerUserId, ...)`, `Rename`, `UpdateFacts`.

**Invariants**

- A custom food is only ever returned to its owner. Enforced in the repository query **and**
  asserted in a slice test — a filter is not an invariant until something proves it.
- Editing or deleting a `Food` has **no effect on existing `LoggedEntry` rows** (FR-012). This is
  the deliberate weak reference from R7.

---

## Aggregate: `FoodLogDay`

One per person per calendar date. Created lazily on the first entry for that date.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | |
| `UserId` | `Guid` | |
| `Date` | `DateOnly` | unique index with `UserId` |
| `TargetSnapshot` | `NutritionTarget` | owned; the target in force that day (FR-005) |
| `Entries` | `IReadOnlyCollection<LoggedEntry>` | owned |

**Behaviour**

| Method | Enforces |
|---|---|
| `static Open(userId, date, target, profileStartedOn, asOf)` | FR-017 — not future, not before profile start |
| `AddEntry(foodId, foodName, factsPer100, quantity, occasion, asOf)` | FR-014, FR-018; scales facts once and stores the snapshot |
| `AmendEntry(entryId, quantity, occasion)` | FR-015 — recalculates from the stored per-unit basis, no duplicate |
| `RemoveEntry(entryId)` | FR-015 |
| `GetTotals()` | FR-016 — sums snapshots only |
| `GetRemaining()` | FR-016 — target minus totals; may be negative |
| `GetBreakdownByOccasion()` | FR-016 |

**Invariants**

- Totals are derived from `Entries` on every read. There is no stored total to fall out of sync.
- A day with zero entries is a *logged day with nothing in it*; a date with no `FoodLogDay` row at
  all is *unlogged*. FR-019 and FR-034 both depend on this distinction, so the read models MUST
  return a three-state answer (outside period / unlogged / logged), never a bare zero.

### Entity: `LoggedEntry` (owned)

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | |
| `FoodLogDayId` | `Guid` | owner FK |
| `FoodId` | `Guid?` | weak reference, **no FK constraint** (R7) |
| `Occasion` | `MealOccasion` | |
| `Quantity` | `Quantity` | owned |
| `Snapshot` | `NutritionSnapshot` | owned — already scaled |
| `RecordedAt` | `DateTime` | |

---

## Aggregate: `Recipe`

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | |
| `OwnerUserId` | `Guid` | always owned by a person |
| `Name` | `string` (≤ 200) | |
| `Servings` | `int` | ≥ 1 |
| `Ingredients` | `IReadOnlyCollection<RecipeIngredient>` | owned, ≥ 1 |

`PerServing(IReadOnlyDictionary<Guid, NutritionFacts> foods)` derives per-serving nutrition (FR-027).
The facts are passed in rather than fetched, keeping the domain free of data access.

### Entity: `RecipeIngredient` (owned)

`Id`, `RecipeId`, `FoodId`, `Quantity` (owned).

Recipes reference foods **live** — unlike logged entries, a recipe is forward-looking, so picking up
a corrected nutrition value is the desired behaviour.

---

## Aggregate: `MealPlan`

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | |
| `UserId` | `Guid` | |
| `StartDate` / `EndDate` | `DateOnly` | `EndDate ≥ StartDate`; span ≤ 31 days |
| `Meals` | `IReadOnlyCollection<PlannedMeal>` | owned |

**Behaviour**: `AddMeal`, `RemoveMeal`, `ProjectDay(date, foods, recipes)` (FR-028),
`BuildShoppingList(foods, recipes)` (FR-029, FR-030), `MarkDayConfirmed(date, asOf)` (FR-031).

**Invariants**

- A `PlannedMeal` references *either* a `FoodId` *or* a `RecipeId`, never both and never neither.
- Confirmation is per-day and one-way; a confirmed day cannot be re-confirmed (guards against
  double-logging under FR-031).
- An unconfirmed plan contributes nothing to intake history (FR-032) — enforced by the plan never
  writing to `FoodLogDay` except through the explicit confirm command.

### Entity: `PlannedMeal` (owned)

`Id`, `MealPlanId`, `Date`, `Occasion`, `FoodId?`, `RecipeId?`, `Quantity?` (for a food),
`Servings?` (for a recipe), `ConfirmedAt` (`DateTime?`).

---

## Business rules (`Domain/Rules/`)

Each is an `IBusinessRule` checked via `CheckRule`, per Principle II. Every one has a test proving it
throws (SC-006).

| Rule | Requirement |
|---|---|
| `MacroSplitMustSumTo100Rule` | R2 |
| `TargetEnergyMustBePositiveRule` | FR-002 |
| `HeightMustBePlausibleRule` | 50-260 cm |
| `WeightMustBePlausibleRule` | FR-025 — 20-500 kg |
| `WeighInCannotBeInFutureRule` | FR-025 |
| `EntryDateCannotBeInFutureRule` | FR-017 |
| `EntryDateCannotPrecedeProfileStartRule` | FR-017 |
| `PortionMustBePositiveRule` | FR-018 |
| `PortionMustBePlausibleRule` | FR-018 — ≤ 5000 units |
| `NutritionValuesCannotBeNegativeRule` | FR-008 |
| `RecipeMustHaveAtLeastOneIngredientRule` | FR-027 |
| `RecipeServingsMustBePositiveRule` | FR-027 |
| `PlanRangeMustBeValidRule` | FR-026 |
| `PlannedMealMustReferenceExactlyOneSourceRule` | FR-026 |
| `DayCannotBeConfirmedTwiceRule` | FR-031 |
| `GoalRateMustBePlausibleRule` | FR-024 — ≤ 1.0 kg/week |

The safety floor is deliberately **not** a rule: FR-004 warns rather than blocks, so it is a query on
`NutritionTarget` (`IsBelowSafeFloor`) plus acknowledgement state on the profile.

---

## Persistence notes

`DietDbContext` maps every aggregate per Principle VI:

- `OwnsOne` for `NutritionTarget`, `NutritionFacts`, `BodyMeasurements`, `WeightGoal`, `Quantity`,
  `NutritionSnapshot`, with explicit column names.
- `OwnsMany` for `WeighIn`, `TargetRecord`, `LoggedEntry`, `RecipeIngredient`, `PlannedMeal`, each
  `.ToTable(...)`, `.WithOwner().HasForeignKey(...)`, `.Property(x => x.Id).ValueGeneratedNever()`,
  `.Ignore(x => x.DomainEvents)`.
- `entity.Navigation(...).UsePropertyAccessMode(PropertyAccessMode.Field)` on every owned collection.
- `entity.Ignore(e => e.DomainEvents)` on every mapped entity.

**Unique indexes**: `DietProfile.UserId`; `(ProfileId, Date)` on `WeighIns`;
`(UserId, Date)` on `FoodLogDays`; `(MealPlanId, Date, Occasion, FoodId, RecipeId)` on
`PlannedMeals`.

**Non-indexes**: `LoggedEntry.FoodId` carries **no** foreign key to `Foods`, by design (R7).

## Domain events

Emitted via `Emit(...)` and dispatched by the context's `SaveChangesAsync` override, matching the
existing pattern: `DietProfileStartedEvent`, `TargetChangedEvent`, `WeightRecordedEvent`,
`GoalAchievedEvent`, `FoodLoggedEvent`, `PlanDayConfirmedEvent`.

No handlers are required by this feature — they exist so that later work (notifications, achievements
on the diet side) has a seam, matching how `JourneyStartedEvent` is emitted today without a consumer.
