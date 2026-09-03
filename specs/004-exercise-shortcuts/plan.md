# Implementation Plan: Exercise Shortcuts

**Branch**: `004-exercise-shortcuts` | **Date**: 2026-09-03 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/004-exercise-shortcuts/spec.md`

## Summary

Let a member save an activity and a duration as a named shortcut, and record that session with one
tap. Saving, tapping, renaming, reordering and removing; nothing suggested and nothing automatic.

This is additive inside `DietApi`. No new service, port or compose change, and one migration adding
a single table.

Two decisions shape the construction. Shortcuts are an **owned collection on `DietPlan`**, not their
own aggregate — the cap and the no-duplicates rule are invariants over the whole set, and a set
invariant needs a consistency boundary that contains the set (R-002). And a shortcut **holds no
estimate**: the figure is computed when the session is recorded, from the activity's current energy
rate and the member's current weight, so a saved button cannot freeze the member's weight at the
moment they saved it (R-003).

## Technical Context

**Language/Version**: C# 13 / .NET 10; TypeScript 5.x / Angular 19

**Primary Dependencies**: Unchanged — ASP.NET Core Minimal APIs, MediatR 12.4.1, EF Core 10
(Sqlite + Design), JwtBearer, OpenAPI + Scalar, `Shared/DDD.BuildingBlocks`. No new backend package
and **no new frontend package** (R-009).

**Storage**: The existing `diet.db` and `DietDbContext`. One migration, `AddExerciseShortcuts`,
adding one table.

**Testing**: xUnit + Shouldly in the existing `DietApi.Tests`, in-memory fakes from `TestSupport/`.

**Target Platform**: Unchanged — Linux container, non-root, port 5000; browser front end via nginx.

**Project Type**: Additive feature within an existing service and an existing Angular programme.

**Performance Goals**: A tap records in one round trip. The shortcut list is capped at ten, so no
read here has a scale problem worth measuring.

**Constraints**: Durations are whole minutes and `int`. A shortcut stores no estimate and no MET.
All instants UTC, calendar days `DateOnly`, every time-dependent domain method takes
`DateTime? asOf = null`. Recording by shortcut must be indistinguishable from recording by hand, and
must move no calorie target and no day's eating assessment.

**Scale/Scope**: 3 user stories, 20 functional requirements, 12 release-gating success criteria plus
2 post-launch measures. 1 new owned entity, ~3 value objects, 6 endpoints, 1 migration, changes to
1 existing Angular component plus a small manage view.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Constitution v1.0.0, ratified 2026-09-01.

### Initial check (before Phase 0)

| Principle | Verdict | How this plan satisfies it |
|--------|--------|--------|
| I. Bounded contexts own their data | PASS | Everything lands inside `DietApi`, which already owns `diet.db`. The shortcut references an `ActivityType` in the same context, and `UserId` still arrives only from the JWT via `IUserService`. |
| II. The domain model holds the rules | PASS | The cap, the no-duplicates rule, position normalisation and the duration bounds all live on the `DietPlan` aggregate and in `IBusinessRule` implementations. Handlers resolve the member, load one aggregate, call one method, persist, map. |
| III. Vertical slices, not layers | PASS | One new `Features/ExerciseShortcuts/` folder in the established shape, plus one new use-case folder under the existing `Features/Exercise/`. |
| IV. Time is a parameter | PASS | `asOf` flows through the date checks on the recording path, unchanged from 002. Shortcuts themselves hold no dates. |
| V. Domain and slice tests ship with the feature | PASS | Every new rule proven to throw, the cap and duplicate rules covered at their boundaries, position normalisation asserted after every mutation, and a test that records the same session by hand and by shortcut and compares them field by field. |
| VI. Migrations and idempotent seeds | PASS | One checked-in migration. No reference data, so nothing to seed and nothing to make idempotent — stated rather than left as an apparent omission. |

**Architecture & Technology Constraints**: the eight obligations apply to *new services*; this adds
none. No port, volume, database file, compose service or frontend prefix is allocated.

**Frontend routing**: no new route and no new programme nav entry. The feature lives on the existing
exercise log, where the recording happens.

**Result**: PASS, no violations. Proceed to Phase 0.

### Post-design re-check (after Phase 1)

Re-evaluated against [data-model.md](./data-model.md) and [contracts/](./contracts/).

| Principle | Verdict | Note |
|--------|--------|--------|
| I | PASS | No design artifact introduces a cross-context read or foreign key. |
| II | PASS | Confirmed: every rule is an aggregate method or an `IBusinessRule`; the handlers only orchestrate. |
| III | PASS | Contract groups map one-to-one onto the feature folders below. |
| IV | PASS | The recording path still takes `asOf`; nothing new reads a clock. |
| V | PASS | Test layout enumerated below; quickstart runs it. |
| VI | PASS | One migration, and the unique index that backs FR-006 is part of it. |

**No new deviations to record.** This feature introduces no departure from the reference
implementation: it adds an owned collection to an aggregate that already has two, and a recording
path that ends in the same aggregate method the existing one does.

**One earlier judgement was reversed**, and it is recorded rather than quietly corrected: before the
spec existed, shortcuts looked like they should be their own aggregate root, by analogy with
`ExerciseDay`. Writing the requirements down showed the opposite — the cap and the no-duplicates
rule are invariants over the set, and a set invariant needs a boundary containing the set. R-002
carries the full argument.

**Result**: PASS. Ready for `/speckit-tasks`.

## Project Structure

### Documentation (this feature)

```text
specs/004-exercise-shortcuts/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── rest-api.md      # Phase 1 output
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks - NOT created here)
```

### Source Code (repository root)

```text
OpenMind.Healthcare/backend/DietApi/
├── Domain/
│   ├── Aggregates/
│   │   └── DietPlan.cs                    # MODIFIED - owns the shortcut collection
│   ├── Entities/
│   │   └── ExerciseShortcut.cs            # NEW - owned by DietPlan
│   ├── Rules/
│   │   └── ExerciseShortcutRules.cs       # NEW - cap, duplicate, name rules
│   └── ValueObjects/
│       └── (none new; duration reuses the existing session rules)
├── Features/
│   ├── ExerciseShortcuts/                 # NEW - /api/exercise-shortcuts
│   │   ├── ExerciseShortcutsEndpoints.cs
│   │   ├── ExerciseShortcutDtos.cs
│   │   ├── GetShortcuts/
│   │   ├── CreateShortcut/
│   │   ├── RenameShortcut/
│   │   ├── ReorderShortcuts/
│   │   └── DeleteShortcut/
│   └── Exercise/
│       └── AddEntryFromShortcut/          # NEW - the one-tap record
└── Infrastructure/Data/
    ├── DietDbContext.cs                   # MODIFIED - one OwnsMany block
    └── Migrations/                        # NEW - AddExerciseShortcuts

OpenMind.Healthcare/backend/DietApi.Tests/
├── Domain/                                # NEW - cap, duplicates, ordering, naming
├── Features/                              # NEW - slice tests, plus the by-hand/by-shortcut match
└── TestSupport/                           # MODIFIED - the plan builder gains shortcuts

OpenMind.Healthcare/frontend/src/app/
├── components/exercise-log/               # MODIFIED - shortcut row, save action, manage view
├── services/diet.service.ts               # MODIFIED - six methods
└── models/diet.models.ts                  # MODIFIED - shortcut wire types
```

**Structure Decision**: Additive within `DietApi` and the existing exercise log. The one structural
judgement is that shortcuts are owned by `DietPlan` rather than standing alone — forced by the set
invariants in FR-006 and FR-007 (R-002), and safe because FR-007 caps the collection at ten.

## Complexity Tracking

> Fill ONLY if Constitution Check has violations that must be justified.

No violations, and no new deviations from the reference implementation. The design reuses patterns
already established and justified: an owned collection on `DietPlan` exactly as `WeightReadings` and
`UnlockedAchievements` are, a unique index backing a domain rule as `WeightReadings` already has,
and a recording path that ends in `ExerciseDay.AddEntry` like the existing one.

The three entries in Complexity Tracking from 001, and the read-model deviation from 003
(ADR 0004), are inherited unchanged and are not re-argued here.

The one thing worth recording for a reader who expects otherwise: **shortcuts are deliberately not
their own aggregate**, despite `ExerciseDay` being one. The difference is that `ExerciseDay` grows
without bound and has no invariant spanning the set of days, whereas a member's shortcuts are capped
at ten and carry two rules that only mean anything across the whole set. Ownership is what makes
those rules enforceable rather than racy.
