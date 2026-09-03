# Phase 1 Data Model: Exercise Shortcuts

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Research**: [research.md](./research.md)

One new owned entity on an existing aggregate. All types live in `DietApi.Domain`.

## Where it sits

```text
DietPlan (aggregate root)                    unchanged, gains one collection
├── BodyMetrics                owned VO
├── Targets                    owned VO
├── WeightReadings[]           owned entity
├── UnlockedAchievements[]     owned entity
└── ExerciseShortcuts[]        owned entity   NEW - at most ten
```

`ExerciseShortcut` references an `ActivityType` by `ActivityTypeId` — a `Guid` column, no navigation
property, no foreign key. It stores no activity name, no MET and no estimate (research R-003).

> **Why owned rather than its own aggregate.** Two rules span the whole set: at most ten shortcuts,
> and no two with the same activity and duration. Neither can be enforced from inside a single
> shortcut without a read-modify-write race. An invariant over a set needs a consistency boundary
> containing the set, and the cap is what makes owning the collection safe.

---

## ExerciseShortcut (owned by DietPlan)

| Field | Type | Notes |
|--------|--------|--------|
| `Id` | `Guid` | `ValueGeneratedNever()`. |
| `DietPlanId` | `Guid` | Owner FK. |
| `ActivityTypeId` | `Guid` | A reference, never a copy. Resolved at read for display and at record for the estimate. |
| `Name` | `string(80)` | Defaulted from the activity and duration; renameable. |
| `DurationMinutes` | `int` | Whole minutes, bounded by the same rules as a session. |
| `Position` | `int` | Contiguous from zero. Assigned only by the aggregate. |
| `CreatedAt` | `DateTime` | UTC. |

**Deliberately absent**: `Met`, `ActivityName`, `EstimatedKcal`, any date. A shortcut is an
instruction to record in future, so it must not carry figures that will be stale when it is used
(FR-010).

---

## DietPlan, extended

**New collection**: `IReadOnlyCollection<ExerciseShortcut> ExerciseShortcuts` over a private field.

| Method | Behaviour |
|--------|--------|
| `SaveExerciseShortcut(activityTypeId, durationMinutes, name, asOf)` | Checks the cap, the duplicate rule and the duration rules. Appends at the end. Returns the new shortcut. |
| `RenameExerciseShortcut(shortcutId, name)` | Checks the name rule. Renaming can never create a duplicate, because duplicates compare activity and duration only (R-007). |
| `ReorderExerciseShortcuts(orderedIds)` | Takes the full ordered list. Refuses a list that is not exactly the member's current shortcuts. Rewrites positions contiguously from zero. |
| `RemoveExerciseShortcut(shortcutId)` | Removes and re-normalises positions so no hole is left. Returns `bool`. |
| `ExerciseShortcut(shortcutId)` | Finds one, or null. |
| `ShortcutsInOrder()` | Ordered by `Position`. |
| `RemainingShortcutSlots` | `MaxShortcuts - count`, so a client can tell a member how many more they may add. |

**Rules** (`Domain/Rules/ExerciseShortcutRules.cs`)

| Rule | Broken when | Requirement |
|--------|--------|--------|
| `ShortcutLimitRule` | The member already has `MaxShortcuts` (10) | FR-007 |
| `ShortcutMustBeUniqueRule` | Another shortcut has the same `ActivityTypeId` and `DurationMinutes` | FR-006 |
| `ShortcutNameMustNotBeEmptyRule` | The name is blank after trimming | FR-004, FR-014 |
| `ShortcutNameWithinLengthRule` | The name exceeds 80 characters | FR-014 |
| `ReorderMustCoverEveryShortcutRule` | The submitted ids are not exactly the member's shortcuts | FR-015 |

Duration is **not** given new rules. `DurationMustBePositiveRule` and `DurationWithinCeilingRule`
from 002 are reused unchanged, which is what makes FR-005 true rather than merely intended: a
duration refused on a session is refused on a shortcut because it is the same rule object.

**Invariants asserted by test**

- After any save, rename, reorder or removal, positions are exactly `0..n-1` with no gaps and no
  duplicates.
- The collection never exceeds `MaxShortcuts`.
- No two shortcuts share an `(ActivityTypeId, DurationMinutes)` pair.

---

## Recording from a shortcut

No new domain type. The recording path resolves the shortcut to an activity and a duration, then
performs exactly what the by-hand path performs:

```text
DietPlan.ExerciseShortcut(id)  ->  activityTypeId, durationMinutes
ActivityType lookup            ->  name, current MET
ExerciseDay.StartDay / AddEntry(activityTypeId, name, met, duration, plan.CurrentWeightKg())
```

The entry snapshots the name, the MET and the estimate at that moment, as every entry does. The
shortcut contributes only the two values a member would otherwise have typed.

---

## Persistence notes

- `OwnsMany` on `DietPlan`, `ToTable("ExerciseShortcuts")`, `WithOwner().HasForeignKey(DietPlanId)`,
  `HasKey(Id)`, `ValueGeneratedNever()`, and `Ignore(e => e.DomainEvents)` — the same shape as
  `WeightReadings`.
- `Navigation(e => e.ExerciseShortcuts).UsePropertyAccessMode(PropertyAccessMode.Field)`.
- Unique index on `(DietPlanId, ActivityTypeId, DurationMinutes)`, so FR-006 holds at the storage
  layer as well as in the domain and a race that slipped past the aggregate still cannot land.
- Supporting index on `(DietPlanId, Position)` for the ordered read.
- `Name` is `string(80)`, `DurationMinutes` and `Position` are `int`. Nothing here is aggregated in
  SQL, so ADR 0002 has nothing to say about it.

## What this feature must NOT do

- **Store an estimate, a MET or an activity name on a shortcut.** The figure is computed when the
  session is recorded (FR-010, SC-003).
- **Relax any recording rule.** Future dates, pre-plan dates, stale day versions and duration
  bounds behave exactly as they do for a typed session (FR-012).
- **Touch a recorded session when a shortcut changes.** Renaming, reordering or deleting a shortcut
  leaves every session recorded from it untouched (FR-017, SC-009).
- **Create, change or remove a shortcut without the member asking.** Nothing is suggested or
  inferred in this release (FR-020, SC-012).
- **Move a calorie target or a day's eating assessment.** Recording by shortcut is recording, and
  carries every guarantee recording already carries (FR-019).

## Requirement coverage

| Requirement group | Carried by |
|--------|--------|
| FR-001 to FR-007 (saving) | `DietPlan.SaveExerciseShortcut`, `ExerciseShortcutRules`, the unique index |
| FR-008 to FR-013 (using) | `AddEntryFromShortcut`, ending in `ExerciseDay.AddEntry` |
| FR-014 to FR-017 (curating) | `RenameExerciseShortcut`, `ReorderExerciseShortcuts`, `RemoveExerciseShortcut` |
| FR-018 to FR-020 (boundaries) | `IUserService` and `.RequireAuthorization()`; the "must NOT" list above |
